/* Original FMOD banks; bounded browser bridge. Source call sites: AudioManager.cs. */
(() => {
  'use strict';
  const base = new URL('audio/', document.baseURI);
  const state = {phase:'loading',enabled:false,version:null,events:0,errors:[],history:[],peakLoops:0,updates:0};
  let F,studio,core,bus,meter,loading,closed=false,music=null,musicPath='',lastRound=-1,roundSerial=-1,musicFilter=null,lastFilter=-1;
  const descriptions=new Map(),loops=new Map(),snapshots=[],counters=new Map(),worldBuses=[];
  const audit=(type,data={})=>{console.log('BF_AUDIO_EVENT',JSON.stringify({type,...data}));state.history.push({type,...data});if(state.history.length>80)state.history.shift();};
  const fail=e=>{if(state.errors.length<10){state.errors.push(String(e));console.error('BF_AUDIO_ERROR',String(e));}};
  const check=(r,name)=>{if(r!==F.OK)throw Error(name+': '+F.ErrorString(r));};
  const get=(obj,method,...args)=>{const out={};check(obj[method](...args,out),method);return out.val===undefined?out:out.val;};
  const vector=p=>({x:p[0]||0,y:p[1]||0,z:p[2]||0});
  const attributes=(p,forward=[0,0,1],up=[0,1,0])=>({position:vector(p),velocity:vector([0,0,0]),forward:vector(forward),up:vector(up)});
  const description=path=>{if(!descriptions.has(path))descriptions.set(path,get(studio,'getEvent',path));return descriptions.get(path);};
  const parameter=(instance,name,value)=>check(instance.setParameterByName(name,value,false),'parameter '+name);
  const position=(instance,p)=>{if(p)check(instance.set3DAttributes(attributes(p)),'position');};
  const instance=(path,p,parameters={})=>{const value=get(description(path),'createInstance');position(value,p);for(const [k,v]of Object.entries(parameters))parameter(value,k,v);return value;};
  const stop=(value,fade=true)=>{check(value.stop(fade?F.STUDIO_STOP_ALLOWFADEOUT:F.STUDIO_STOP_IMMEDIATE),'stop');check(value.release(),'release event');};
  function oneShot(command,packet){
    if(!state.enabled)return;
    const params={...(command.parameters||{})};
    if(command.counter!==undefined){
      let count=counters.get(command.path);
      if(!count){count={count:0,time:packet.time,cooldown:command.counter};counters.set(command.path,count);}
      else{count.count=Math.fround(packet.unscaled-count.time)<count.cooldown?count.count+1:0;count.time=packet.unscaled;}
      params.counter=count.count;
    }
    const value=instance(command.path,command.position,params);
    check(value.start(),'oneshot start');check(value.release(),'oneshot release');
    audit('oneshot',{path:command.path,parameters:params});state.events++;
  }
  function syncLoops(wanted){
    const keep=new Set();
    for(const item of wanted){
      keep.add(item.key);let entry=loops.get(item.key);
      if(entry&&entry.path!==item.path){stop(entry.value);loops.delete(item.key);entry=null;}
      if(!entry){if(loops.size>=128)throw Error('Audio loop budget exceeded');entry={path:item.path,value:instance(item.path,item.position,item.parameters)};check(entry.value.start(),'loop start');loops.set(item.key,entry);audit('loop start',{key:item.key,path:item.path});}
      else{position(entry.value,item.position);for(const [name,value]of Object.entries(item.parameters||{}))parameter(entry.value,name,value);}
    }
    for(const [key,entry]of loops)if(!keep.has(key)){stop(entry.value);loops.delete(key);audit('loop stop',{key});}
    state.peakLoops=Math.max(state.peakLoops,loops.size);
  }
  async function prepare(){
    if(loading)return loading;
    loading=(async()=>{
      F=await FMODModule({INITIAL_MEMORY:128*1024*1024,wasmMemory:new WebAssembly.Memory({initial:2048,maximum:2048}),locateFile:name=>new URL(name,base).href,print:()=>{},printErr:fail});
      if(closed)return;
      studio=get(F,'Studio_System_Create');core=get(studio,'getCoreSystem');
      const version={};check(core.getVersion(version,{}),'version');state.version=version.val.toString(16);
      check(core.setDSPBufferSize(2048,2),'buffer');
      check(studio.initialize(128,F.STUDIO_INIT_NORMAL,F.INIT_NORMAL,null),'initialize');
      // Two requests at most, no worker, no retry loop. Source bank bytes are unmodified.
      const names=['Master Bank.strings.bank','Master Bank.bank'];
      const bytes=await Promise.all(names.map(async name=>{const response=await fetch(new URL(name,base));if(!response.ok)throw Error('bank HTTP '+response.status);return new Uint8Array(await response.arrayBuffer());}));
      if(closed)return;
      for(let i=0;i<names.length;i++){F.FS_createDataFile('/',names[i],bytes[i],true,false);const bank=get(studio,'loadBankFile','/'+names[i],F.STUDIO_LOAD_BANK_NORMAL);if(i===1)state.bankEvents=get(bank,'getEventCount');}
      bus=get(studio,'getBus','bus:/');check(bus.setMute(true),'initial mute');
      for(const path of ['bus:/SFX/Diegetic','bus:/SFX/Atmos']){const out={};const result=studio.getBus(path,out);if(result===F.OK)worldBuses.push(out.val);else if(result!==F.ERR_EVENT_NOTFOUND)check(result,'world bus');}
      const globalDescription={};state.stingAvailable=studio.getParameterDescriptionByName('sting',globalDescription)===F.OK;
      meter=get(get(core,'getMasterChannelGroup'),'getDSP',0);check(meter.setMeteringEnabled(false,true),'metering');state.peakMax=0;
      for(const [name,param,value]of [['SFXVolume','sfxBusVolume',1],['MusicVolume','musicBusVolume',1],['MusicFilter','musicFilter',0]]){
        const snap=instance('snapshot:/'+name,null,{[param]:value});check(snap.start(),'snapshot');snapshots.push(snap);if(name==='MusicFilter')musicFilter=snap;
      }
      const time=instance('snapshot:/TimeScale');check(time.start(),'time snapshot');snapshots.push(time);
      check(studio.setParameterByName('timeScale',1,false),'timeScale');
      state.phase='ready';audit('ready');console.log('BF_AUDIO_READY',JSON.stringify({version:state.version,bank_events:state.bankEvents,heap_bytes:128*1024*1024}));
    })().catch(e=>{state.phase='error';fail(e);shutdown(false);});
    return loading;
  }
  function toggle(){
    if(state.phase!=='ready')return false;
    try{check(core.mixerSuspend(),'suspend');check(core.mixerResume(),'resume');state.enabled=!state.enabled;check(bus.setMute(!state.enabled),'mute');audit('enabled',{value:state.enabled});return state.enabled;}
    catch(e){fail(e);return false;}
  }
  function frame(json){
    if(state.phase!=='ready'||closed)return;
    try{
      const packet=JSON.parse(json);
      if(packet.round_serial!==roundSerial){for(const entry of loops.values())stop(entry.value);loops.clear();for(const worldBus of worldBuses)check(worldBus.stopAllEvents(F.STUDIO_STOP_ALLOWFADEOUT),'world sounds');roundSerial=packet.round_serial;audit('round reset',{serial:roundSerial});}
      if(packet.listener)check(studio.setListenerAttributes(0,attributes(packet.listener.position,packet.listener.forward,packet.listener.up),null),'listener');
      check(studio.setParameterByName('timeScale',packet.scale,false),'timeScale');
      // The shipped native bank also returns ERR_EVENT_NOTFOUND for obsolete sting.
      if(state.stingAvailable)check(studio.setParameterByName('sting',packet.sting?1:0,false),'sting');
      if(musicFilter&&lastFilter!==(packet.music_filter||0)){lastFilter=packet.music_filter||0;parameter(musicFilter,'musicFilter',lastFilter);audit('music filter',{value:lastFilter});}
      if(packet.stop_music&&music){stop(music);music=null;musicPath='';}
      if(!packet.stop_music&&packet.music&&packet.music!==musicPath){if(music)stop(music);music=instance(packet.music);musicPath=packet.music;check(music.start(),'music start');audit('music',{path:musicPath});lastRound=-1;}
      if(music){parameter(music,'musicSection',packet.section);if(musicPath!=='event:/music/menu'&&lastRound!==packet.round_end){parameter(music,'roundEnd',packet.round_end);lastRound=packet.round_end;audit('round_end',{value:lastRound});}}
      for(const command of packet.commands)oneShot(command,packet);
      syncLoops(state.enabled?packet.loops:[]);
      check(studio.update(),'update');state.updates++;
      if(state.updates%12===0){const info={peaklevel:[],rmslevel:[]};check(meter.getMeteringInfo(null,info),'meter');const peak=Math.max(0,...info.peaklevel);if(peak>0&&state.peakMax===0)console.log('BF_AUDIO_SIGNAL',JSON.stringify({peak,channels:info.numchannels,samples:info.numsamples}));state.peakMax=Math.max(state.peakMax,peak);}
    }catch(e){state.phase='error';fail(e);shutdown(false);}
  }
  function shutdown(close=true){
    if(close)closed=true;
    if(studio){try{check(studio.release(),'shutdown');}catch(e){fail(e);}studio=null;}
    loops.clear();snapshots.length=0;descriptions.clear();music=null;
  }
  window.BFAudio={prepare,toggle,frame,status:()=>JSON.stringify({...state,loops:loops.size}),shutdown};
  window.addEventListener('pagehide',()=>shutdown(),{once:true});
  prepare();
})();
