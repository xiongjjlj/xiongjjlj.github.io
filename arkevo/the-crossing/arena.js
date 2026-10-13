import * as THREE from './assets/three.module.js';
import {sceneState,routeAt,ROUTE,OBSERVERS,SMOKES,EXIT} from './scene-model.js';
export {sceneState,VALIDATION,validateLayout} from './scene-model.js';

// An original, deterministic training environment. All geometry is generated here.
export function createArena(container, {quality='high', mode='cinematic'}={}) {
  const scene = new THREE.Scene();
  scene.background = new THREE.Color('#c7cec8');
  scene.fog = new THREE.Fog('#c7cec8', 33, 76);
  const renderer = new THREE.WebGLRenderer({antialias: quality === 'high', alpha:false, powerPreference:'high-performance'});
  renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, quality === 'high' ? 1.8 : 1));
  renderer.shadowMap.enabled = quality === 'high';
  renderer.shadowMap.type = THREE.PCFSoftShadowMap;
  renderer.outputColorSpace = THREE.SRGBColorSpace;
  renderer.toneMapping = THREE.ACESFilmicToneMapping;
  renderer.toneMappingExposure = .94;
  renderer.domElement.style.cssText = 'display:block;width:100%;height:100%;outline:none;';
  container.appendChild(renderer.domElement);
  const camera = new THREE.PerspectiveCamera(39, 1, .1, 100);
  const hemi = new THREE.HemisphereLight('#f7f6e7','#647361',1.75); scene.add(hemi);
  const sun = new THREE.DirectionalLight('#fff0d2',2.6); sun.position.set(-14,26,15); sun.castShadow=true;
  sun.shadow.mapSize.set(2048,2048); Object.assign(sun.shadow.camera,{left:-25,right:25,top:24,bottom:-24,near:.5,far:70});
  sun.shadow.normalBias=.025; sun.shadow.bias=-.00015; sun.shadow.radius=4; scene.add(sun);
  const fill=new THREE.DirectionalLight('#c2d4cd',.42); fill.position.set(12,10,-20);scene.add(fill);
  const world=new THREE.Group(); scene.add(world);
  const materials=[]; const geometries=[]; const textures=[];
  function material(color, roughness=.85, extra={}){const m=new THREE.MeshStandardMaterial({color,roughness,...extra});materials.push(m);return m;}
  const sand=material('#cbbb98'), sandDark=material('#a99a77'), cap=material('#ded0ad'), concrete=material('#a5aaa1'), stone=material('#b9bcae');
  const road=material('#878e83'), metal=material('#424d47',.6), teal=material('#236852',.5), black=material('#23312c'), wood=material('#8c7e61');
  const windowMat=material('#465753',.32), paint=material('#e8e4cf'), rust=material('#786b54');
  function mesh(geo, mat, parent=world){geometries.push(geo);const m=new THREE.Mesh(geo,mat);m.castShadow=true;m.receiveShadow=true;parent.add(m);return m;}
  function box(w,h,d,x,y,z,mat=sand,parent=world){const m=mesh(new THREE.BoxGeometry(w,h,d),mat,parent);m.position.set(x,y,z);return m;}
  function cyl(r1,r2,h,x,y,z,mat,parent=world,n=16){const m=mesh(new THREE.CylinderGeometry(r1,r2,h,n),mat,parent);m.position.set(x,y,z);return m;}
  function line(points,color,opacity=1,parent=world){const g=new THREE.BufferGeometry().setFromPoints(points.map(p=>new THREE.Vector3(...p)));geometries.push(g);const m=new THREE.LineBasicMaterial({color,transparent:true,opacity,depthWrite:false});materials.push(m);const l=new THREE.Line(g,m);parent.add(l);return l;}
  function stroke(points,color,width=.04,opacity=1,parent=toolLayer){
    const curve=new THREE.CatmullRomCurve3(points.map(p=>new THREE.Vector3(...p)),false,'centripetal');
    const segments=Math.max(24,(points.length-1)*2), radial=6;
    const g=new THREE.TubeGeometry(curve,segments,width,radial,false);geometries.push(g);
    const m=new THREE.MeshBasicMaterial({color,transparent:true,opacity,depthWrite:false});materials.push(m);
    const l=new THREE.Mesh(g,m);l.userData={fullCount:g.index.count,step:radial*6};parent.add(l);return l;
  }
  function reveal(l,progress){const n=l.userData.fullCount || l.geometry.attributes.position.count, step=l.userData.step || 1;l.geometry.setDrawRange(0,Math.floor(Math.max(0,Math.min(1,progress))*n/step)*step);}
  function label(text,w=2.8,h=.6,color='#efe7d0',bg='#354b42'){
    const c=document.createElement('canvas');c.width=512;c.height=128;const ctx=c.getContext('2d');ctx.fillStyle=bg;ctx.fillRect(0,0,512,128);ctx.strokeStyle='rgba(255,255,255,.25)';ctx.lineWidth=3;ctx.strokeRect(8,8,496,112);ctx.fillStyle=color;ctx.font='500 55px ui-monospace, monospace';ctx.textAlign='center';ctx.textBaseline='middle';ctx.fillText(text,256,66);const t=new THREE.CanvasTexture(c);t.colorSpace=THREE.SRGBColorSpace;textures.push(t);const m=material('#ffffff',.85,{map:t});return mesh(new THREE.PlaneGeometry(w,h),m);
  }
  // Fine sand texture, generated once, with no external art dependencies.
  const texCanvas=document.createElement('canvas');texCanvas.width=128;texCanvas.height=128;
  const tc=texCanvas.getContext('2d');tc.fillStyle='#c0b99f';tc.fillRect(0,0,128,128);
  let seed=427;const rnd=()=>{seed=(seed*16807)%2147483647;return (seed-1)/2147483646;};
  for(let i=0;i<8000;i++){tc.fillStyle=`rgba(${rnd()>.5?'255,255,255':'40,45,36'},${rnd()*.10})`;tc.fillRect(rnd()*128,rnd()*128,1,1);}
  const groundTexture=new THREE.CanvasTexture(texCanvas);groundTexture.wrapS=groundTexture.wrapT=THREE.RepeatWrapping;groundTexture.repeat.set(14,14);groundTexture.colorSpace=THREE.SRGBColorSpace;textures.push(groundTexture);
  const groundMat=material('#e2ddc8',.95,{map:groundTexture});
  box(30,.65,24,0,-.42,0,concrete);box(29.9,.14,23.9,0,-.025,0,groundMat);
  // Broad cross lane and inset drainage channels.
  box(29.2,.025,5.6,0,.06,0,road);box(28.8,.06,.16,0,.085,2.95,concrete);box(28.8,.06,.16,0,.085,-2.95,concrete);
  for(let x=-13.6;x<14;x+=2.5){box(1.15,.02,.08,x,.088,0,paint);}
  for(let x=-14;x<15;x+=1.2){box(.035,.016,2,x,.075,4.05,sandDark);box(.035,.016,2,x,.075,-4.05,sandDark);}
  for(let x=-13;x<15;x+=5){box(.72,.018,.42,x,.077,2.55,metal);for(let j=0;j<5;j++)box(.035,.023,.36,x-.27+j*.13,.087,2.55,concrete);}
  // Buildings form a legible, asymmetrical junction with large openings.
  function building(x,z,w,d,h,shade=sand){
    const b=new THREE.Group();world.add(b);b.position.set(x,0,z);
    box(w,h,d,0,h/2,0,shade,b);box(w+.18,.2,d+.18,0,h-.02,0,cap,b);box(w+.3,.13,d+.3,0,h+.15,0,sandDark,b);
    box(w+.1,.28,d+.1,0,.18,0,sandDark,b);
    box(w,.08,.06,0,1.05,d/2+.03,cap,b);box(w,.08,.06,0,h-.75,d/2+.03,cap,b);
    for(let i=0;i<Math.floor(w/2.2);i++){
      const xx=-w/2+1.2+i*2.2;box(.92,1.3,.05,xx,h-1.9,d/2+.045,windowMat,b);
      box(1.08,.1,.22,xx,h-2.57,d/2+.1,cap,b);box(.04,1.3,.08,xx,h-1.9,d/2+.09,metal,b);
      box(.92,.04,.08,xx,h-1.9,d/2+.09,metal,b);
    }
    for(let i=0;i<3;i++) box(.055,.12,d+.12,0,.8+i*.67,0,sandDark,b);
    // Low roof parapets and rooftop mechanical details.
    box(w+.12,.42,.14,0,h+.38,d/2,cap,b);box(w+.12,.42,.14,0,h+.38,-d/2,cap,b);
    box(.14,.42,d,w/2,h+.38,0,cap,b);box(.14,.42,d,-w/2,h+.38,0,cap,b);
    box(1.2,.7,.9,.4,h+.55,-.3,concrete,b);for(let j=0;j<5;j++)box(.98,.024,.03,.4,h+.35+j*.08,.16,metal,b);
    cyl(.17,.17,.9,-w/2+.6,h+.58,-d/2+.7,metal,b);
    return b;
  }
  building(-10,-7.0,6.2,5.8,4.8);building(-3.0,-8.7,6.8,2.5,5.8,sandDark);building(8.6,-8.0,9.0,4.8,5.3);
  building(-11,8,5.2,5.7,3.9);building(10.6,8.5,6.2,4.4,4.25,sandDark);
  // Monumental gate with a real opening and a curved top.
  const gate=new THREE.Group();world.add(gate);gate.position.set(2.0,0,-7.25);
  box(1.35,4.9,1.25,-2.0,2.45,0,sand,gate);box(1.35,4.9,1.25,2.0,2.45,0,sand,gate);box(5.4,1.1,1.25,0,4.45,0,sand,gate);
  const archShape=new THREE.Shape();archShape.absarc(0,2.6,1.32,0,Math.PI,false);archShape.lineTo(-1.32,3.9);archShape.lineTo(1.32,3.9);archShape.closePath();
  const arch=mesh(new THREE.ExtrudeGeometry(archShape,{depth:1.25,bevelEnabled:false}),sand,gate);arch.position.z=-.625;
  for(let i=0;i<11;i++){let a=Math.PI*i/10;let b=box(.32,.18,1.4,Math.cos(a)*1.43,2.6+Math.sin(a)*1.43,0,cap,gate);b.rotation.z=a-Math.PI/2;}
  box(5.6,.18,1.55,0,5.04,0,cap,gate);
  const sign=label('EXIT  /  07',3.2,.57,'#edf2dc','#315947');sign.position.set(2,4.53,-6.58);
  const wallSign=label('ARKEVO',3.9,.85);wallSign.position.set(-10,2.8,-4.04);
  const caution=label('ADAPTATION RANGE',3.6,.45,'#414a3c','#d2c39c');caution.position.set(9,1.17,-5.53);
  // Shaded walkway, stair access and railings.
  for(let i=0;i<7;i++)box(2.5,.26*(i+1),.45,7.2,.13*(i+1),-4.4-i*.45,stone);
  box(2.65,.15,1.7,7.2,1.92,-7.75,stone);
  line([[5.83,.8,-4.2],[5.83,2.85,-7.6]],'#4d5b4e');for(let i=0;i<5;i++)cyl(.025,.025,.87,5.83,.44+i*.31,-4.4-i*.55,metal);
  for(let x=-12;x<=-8;x+=2){box(.11,2.7,.11,x,1.35,5.1,metal);}
  box(5.4,.12,1.9,-10,2.71,5.5,wood);
  for(let x=-12.5;x<-7.4;x+=.24)box(.09,.055,2.1,x,2.81,5.5,cap);
  // Cover props: inset framed crates, oil drums, utility boxes and low barricades.
  function crate(x,z,s=1,y=0){const p=new THREE.Group();world.add(p);p.position.set(x,y,z);box(s,s,s,0,s/2,0,wood,p);const e=.085;for(let a of [-1,1])for(let b of [-1,1]){box(e,s+.04,e,a*s/2,s/2,b*s/2,sandDark,p);box(s+.04,e,e,0,(a+1)*s/2,b*s/2,cap,p);box(e,e,s+.04,a*s/2,(b+1)*s/2,0,cap,p);}box(s*.9,.08,.025,0,s*.4,s/2+.025,metal,p);return p;}
  crate(-6,3.9,1.4);crate(-7.42,3.9,1.4);crate(-6.75,3.92,1.1,1.4);crate(4.7,3.85,1.65);crate(6.5,3.8,1.1);crate(10.2,-3.8,1.3);crate(11.7,-3.8,1.3);crate(11,-3.8,1.08,1.3);
  for(const [x,z] of [[-9,-3.7],[-8.18,-3.7],[8.1,4.45]]){cyl(.35,.35,.95,x,.48,z,teal);cyl(.365,.365,.06,x,.16,z,metal);cyl(.365,.365,.06,x,.78,z,metal);}
  box(3.8,.72,.48,-1.9,.36,4.4,concrete);box(3.96,.1,.6,-1.9,.76,4.4,cap);
  box(.8,1.3,.6,-12,.65,-3.9,metal);box(.68,.05,.61,-12,1.15,-3.9,cap);
  // Antennae, cables, lamps, pavement marks and scattered foliage.
  cyl(.035,.035,2.7,-8,6.05,-8.3,metal);line([[-8,7.1,-8.3],[-6.8,7.1,-8.3]],'#455146');
  const cable=[];for(let i=0;i<40;i++){const a=i/39;cable.push([-11+22*a,5.4-1.0*Math.sin(a*Math.PI),-5]);}line(cable,'#4d5144',.8);
  for(const [x,z] of [[-13,3.4],[12,3.5]]){cyl(.045,.06,3.9,x,1.95,z,metal);box(.5,.08,.25,x+.18,3.9,z,metal);box(.32,.025,.17,x+.18,3.85,z,paint);}
  const leaf=material('#707961');for(const [x,z] of [[-13.8,-10],[-5.6,-10.7],[13.5,10.5],[-13,10]])for(let i=0;i<12;i++){const m=mesh(new THREE.ConeGeometry(.16,.55,4),leaf);m.position.set(x+(rnd()-.5)*1.4,.22,z+(rnd()-.5)*1.1);m.rotation.z=(rnd()-.5)*.8;}
  const mark=label('START',2.6,.65,'#edf0df','#5b7160');mark.rotation.x=-Math.PI/2;mark.position.set(-10,.09,3.5);
  box(1.7,.018,1.7,EXIT.x,.075,EXIT.z,teal);const exitMark=label('EXIT',1.35,.46,'#edf2dc','#315947');exitMark.rotation.x=-Math.PI/2;exitMark.position.set(EXIT.x,.092,EXIT.z);
  // Articulated original robots; joint angles are driven only by timeline time.
  function robot(color){const g=new THREE.Group();world.add(g);const bodyMat=material(color,.5);const dark=material('#273831',.5);const torso=box(.43,.53,.29,0,1.02,0,bodyMat,g);box(.25,.25,.27,0,1.44,0,bodyMat,g);box(.23,.06,.03,0,1.47,.145,black,g);box(.15,.018,.033,0,1.47,.165,paint,g);box(.35,.14,.26,0,.69,0,dark,g);box(.25,.36,.13,0,1.0,-.2,dark,g);
    const limbs=[];for(let side of [-1,1]){const leg=new THREE.Group();g.add(leg);leg.position.set(side*.13,.68,0);box(.13,.53,.15,0,-.27,0,dark,leg);box(.16,.26,.18,0,-.40,0,bodyMat,leg);box(.16,.09,.29,0,-.61,.045,dark,leg);limbs.push(leg);const arm=new THREE.Group();g.add(arm);arm.position.set(side*.3,1.23,0);box(.13,.48,.14,0,-.19,0,bodyMat,arm);cyl(.09,.09,.12,0,.0,0,dark,arm);limbs.push(arm);}
    // Sensor/tool held across the body.
    box(.1,.10,.6,.19,1.01,.3,dark,g);return {g,limbs};
  }
  const actor=robot('#d9ddd1');const enemyA=robot('#82675b');const enemyB=robot('#82675b');const enemyC=robot('#82675b');
  [enemyA,enemyB,enemyC].forEach((enemy,i)=>{enemy.g.position.set(OBSERVERS[i].x,0,OBSERVERS[i].z);enemy.g.rotation.y=-1.6;});
  // Tool layers use calm, readable lines on top of the actual environment.
  const toolLayer=new THREE.Group();world.add(toolLayer);
  function ring(r,color){const m=new THREE.MeshBasicMaterial({color,transparent:true,opacity:.75,depthWrite:false,side:THREE.DoubleSide});materials.push(m);const n=mesh(new THREE.RingGeometry(r-.025,r+.025,64),m,toolLayer);n.rotation.x=-Math.PI/2;n.position.y=.115;n.castShadow=false;return n;}
  const rings=[ring(SMOKES[0].radius,'#377c64'),ring(SMOKES[1].radius,'#377c64'),ring(.25,'#4a9676')];rings[0].position.set(SMOKES[0].x,.115,SMOKES[0].z);rings[1].position.set(SMOKES[1].x,.115,SMOKES[1].z);
  const arrivalRing=ring(.95,'#468a65');arrivalRing.position.set(EXIT.x,.13,EXIT.z);const verifiedFlag=label('EXIT VERIFIED',3.55,.6,'#edf2dc','#315947');world.remove(verifiedFlag);toolLayer.add(verifiedFlag);verifiedFlag.position.set(2,5.83,-7.1);
  const smokeLabels=SMOKES.map(smoke=>{const l=label(smoke.id,1.0,.4,'#edf0dc','#38634e');world.remove(l);toolLayer.add(l);l.position.set(smoke.x,3.1,smoke.z);return l;});
  const observerLabels=OBSERVERS.map(o=>{const l=label(o.id,.6,.3,'#eee5d9','#895f50');world.remove(l);toolLayer.add(l);l.position.set(o.x,1.99,o.z);return l;});
  const paths=[];
  function arc(start,end,peak,color,opacity=1,thick=false){const pts=[];for(let i=0;i<=70;i++){const q=i/70;pts.push([THREE.MathUtils.lerp(start[0],end[0],q),THREE.MathUtils.lerp(start[1],end[1],q)+Math.sin(q*Math.PI)*peak,THREE.MathUtils.lerp(start[2],end[2],q)]);}const l=thick?stroke(pts,color,.033,opacity,toolLayer):line(pts,color,opacity,toolLayer);l.geometry.setDrawRange(0,0);paths.push(l);return l;}
  const throwOne=arc([-6.5,1.4,2.2],[SMOKES[0].x,.15,SMOKES[0].z],4.8,'#c1c892',.95,true);
  const throwTwo=arc([-6.5,1.4,2.2],[SMOKES[1].x,.15,SMOKES[1].z],5.8,'#438568',.95,true);
  const candidates=[];for(let j=0;j<9;j++)candidates.push(arc([-6.5,1.4,2.2],[2.1+j*.3,.15,-4.5+j*.22],4.4+j*.23,'#69a48b',.23));
  const safePoints=Array.from({length:101},(_,i)=>{const p=routeAt(i/100);return [p.x,.15,p.z];});
  const safeLine=stroke(safePoints,'#3e8c69',.055,.92,toolLayer);
  const rayMaterial=new THREE.MeshBasicMaterial({color:'#b3604e',transparent:true,opacity:.85,depthWrite:false});materials.push(rayMaterial);
  const rays=OBSERVERS.map(()=>mesh(new THREE.CylinderGeometry(.022,.022,1,8),rayMaterial,toolLayer));rays.forEach(r=>{r.castShadow=false;r.renderOrder=4;});
  const hitMaterial=new THREE.MeshBasicMaterial({color:'#4b9b79',depthWrite:false});materials.push(hitMaterial);
  const hits=OBSERVERS.map(()=>mesh(new THREE.SphereGeometry(.105,12,8),hitMaterial,toolLayer));hits.forEach(h=>{h.castShadow=false;h.renderOrder=5;});
  const hitGround=OBSERVERS.map(()=>ring(.22,'#4b9b79'));
  const sightUp=new THREE.Vector3(0,1,0),sightA=new THREE.Vector3(),sightB=new THREE.Vector3();
  function updateSight(l,a,b){sightA.set(a.x,a.y,a.z);sightB.set(b.x,b.y,b.z);const delta=sightB.clone().sub(sightA);l.position.copy(sightA).add(sightB).multiplyScalar(.5);l.scale.y=delta.length();l.quaternion.setFromUnitVectors(sightUp,delta.normalize());}
  const grenade=mesh(new THREE.SphereGeometry(.095,12,12),metal);grenade.castShadow=true;
  const halo=ring(.43,'#eee8ce');
  // Procedural radial alpha: camera-facing soft particles, no external images.
  const smokeCanvas=document.createElement('canvas');smokeCanvas.width=256;smokeCanvas.height=256;
  const smokeCtx=smokeCanvas.getContext('2d');const soft=smokeCtx.createRadialGradient(128,128,0,128,128,128);
  soft.addColorStop(0,'rgba(255,255,255,1)');soft.addColorStop(.25,'rgba(255,255,255,.95)');soft.addColorStop(.55,'rgba(255,255,255,.64)');soft.addColorStop(.8,'rgba(255,255,255,.20)');soft.addColorStop(1,'rgba(255,255,255,0)');smokeCtx.fillStyle=soft;smokeCtx.fillRect(0,0,256,256);
  const smokeTex=new THREE.CanvasTexture(smokeCanvas);textures.push(smokeTex);
  const smokeGroup=new THREE.Group();world.add(smokeGroup);const smokeClusters=[];
  for(let c=0;c<2;c++){const group=new THREE.Group();smokeGroup.add(group);group.position.set(SMOKES[c].x,0,SMOKES[c].z);const blobs=[];
    for(let j=0;j<36;j++){
      const mat=new THREE.SpriteMaterial({map:smokeTex,color:j%3===0?'#c5cdc0':j%3===1?'#adb8a9':'#b8c2b1',transparent:true,opacity:0,depthWrite:false,depthTest:true,fog:true});materials.push(mat);
      const b=new THREE.Sprite(mat);group.add(b);const a=rnd()*Math.PI*2,r=rnd()*.82;
      b.userData={base:new THREE.Vector3(Math.cos(a)*r,.65+rnd()*1.3,Math.sin(a)*r),phase:rnd()*10,size:2.15+rnd()*.85};
      b.scale.setScalar(b.userData.size);blobs.push(b);
    }smokeClusters.push({group,blobs});
  }
  const clamp=THREE.MathUtils.clamp;const smooth=(a,b,t)=>{const x=clamp((t-a)/(b-a),0,1);return x*x*(3-2*x);};
  function pathAt(q){const p=routeAt(q);return new THREE.Vector3(p.x,0,p.z);}
  function resize(){const w=container.clientWidth||1280,h=container.clientHeight||720;renderer.setSize(w,h,false);camera.aspect=w/h;camera.updateProjectionMatrix();}
  resize();let observer;if(typeof ResizeObserver!=='undefined'){observer=new ResizeObserver(resize);observer.observe(container);}
  function render(time, opts={}){
    const t=clamp(Number(time)||0,0,84),hack=opts.hack!==false,currentMode=opts.mode||mode;
    const state=sceneState(t),q=state.q;
    const pos=pathAt(q);actor.g.position.copy(pos);const ahead=pathAt(Math.min(1,q+.025));const heading=q>.999?Math.PI:Math.atan2(ahead.x-pos.x,ahead.z-pos.z);actor.g.rotation.set(0,heading,0);
    const walking=state.walking;const gait=walking?Math.sin(t*8)*.43:0;actor.limbs[0].rotation.x=gait;actor.limbs[2].rotation.x=-gait;actor.limbs[1].rotation.x=-gait*.5-.25;actor.limbs[3].rotation.x=gait*.5-.55;
    if(state.failed){actor.g.rotation.z=-.62;actor.g.position.y=-.15;}
    else actor.g.position.y=walking?Math.abs(Math.sin(t*8))*.025:0;
    halo.position.set(pos.x,.13,pos.z);halo.material.opacity=.35+.12*Math.sin(t*3);
    const firstStrength=state.smokes[0].strength,secondStrength=state.smokes[1].strength;
    smokeClusters.forEach(({group,blobs},i)=>{const strength=i===0?firstStrength:secondStrength;group.visible=strength>.005;group.scale.setScalar((.08+strength*.92)*SMOKES[i].radius/2.4);blobs.forEach((b,j)=>{b.position.copy(b.userData.base);b.position.x+=Math.sin(t*.33+j)*.09;b.position.y+=Math.sin(t*.38+j)*.08;b.position.z+=Math.cos(t*.28+j)*.09;b.material.rotation=.05*Math.sin(t*.09+j);b.scale.setScalar(b.userData.size*(1+.035*Math.sin(t*.3+j)));b.material.opacity=strength*(.24+(j%3)*.018);});});
    toolLayer.visible=hack;const design=t>=18;
    const firstArc=smooth(28,34,t);throwOne.visible=t>=28&&t<50;reveal(throwOne,firstArc);
    throwTwo.visible=t>=53;reveal(throwTwo,smooth(53,59,t));
    candidates.forEach((l,j)=>{l.visible=t>=50&&t<64;l.geometry.setDrawRange(0,Math.floor(smooth(50+j*.25,56+j*.25,t)*71));l.material.opacity=.09+.16*(.5+.5*Math.sin(t*1.8-j*.6));});
    rings[0].visible=t>=28;rings[1].visible=t>=53;rings[0].material.opacity=t>=50?.35:.7;rings[1].material.opacity=.5+.2*Math.sin(t*2);
    rings[2].position.set(pos.x,.145,pos.z);rings[2].visible=design;
    safeLine.visible=t>=59;safeLine.material.opacity=.72+.12*Math.sin(t*1.6);reveal(safeLine,t>=64?1:smooth(59,63,t));
    state.rays.forEach((ray,i)=>{rays[i].visible=true;updateSight(rays[i],ray.from,ray.end);hits[i].visible=ray.status==='smoke';hitGround[i].visible=ray.status==='smoke';hits[i].position.set(ray.end.x,ray.end.y,ray.end.z);hitGround[i].position.set(ray.end.x,.15,ray.end.z);});
    arrivalRing.material.opacity=state.reached?.9:.3;arrivalRing.scale.setScalar(state.reached?1+.06*Math.sin(t*3):1);verifiedFlag.visible=state.reached;
    smokeLabels.forEach((l,i)=>{l.visible=state.smokes[i].strength>.4;});
    grenade.visible=t>=28&&t<34||t>=53&&t<59;
    if(grenade.visible){const s=t<34?firstArc:smooth(53,59,t),ex=SMOKES[t<34?0:1].x,ez=SMOKES[t<34?0:1].z,peak=t<34?4.8:5.8;grenade.position.set(THREE.MathUtils.lerp(-6.5,ex,s),THREE.MathUtils.lerp(1.4,.15,s)+Math.sin(s*Math.PI)*peak,THREE.MathUtils.lerp(2.2,ez,s));}
    actor.g.visible=currentMode!=='pov';
    if(currentMode==='top'){camera.fov=35;camera.position.set(0,36,8);camera.up.set(0,1,0);camera.lookAt(0,0,-.3);}
    else if(currentMode==='pov'){camera.fov=66;camera.position.set(pos.x,1.48,pos.z);const target=pathAt(Math.min(1,q+.12));target.y=1.2;if(q>.999)target.set(EXIT.x,1.35,EXIT.z-2);if(t>=18&&t<64)target.set(2.2,1.35,-1.5);camera.lookAt(target);}
    else {camera.fov=39;const orbit=.055*Math.sin(t*.035);camera.position.set(19.2+Math.sin(orbit)*16,18.3+.55*Math.sin(t*.018),23.4+Math.cos(orbit)*.8);camera.lookAt(.1,1.1,-1.0);}
    // The first-person view loses visibility inside a smoke volume.
    let occlusion=0;
    if(currentMode==='pov')smokeClusters.forEach(({group},i)=>{const strength=i===0?firstStrength:secondStrength;const distance=Math.hypot(pos.x-group.position.x,pos.z-group.position.z);occlusion=Math.max(occlusion,strength*(1-smooth(.55,SMOKES[i].radius*.83,distance)));});
    scene.fog.near=THREE.MathUtils.lerp(33,.12,occlusion);scene.fog.far=THREE.MathUtils.lerp(76,3.0,occlusion);scene.fog.color.set(occlusion>.15?'#b6c0b0':'#c7cec8');
    [...smokeLabels,...observerLabels].forEach(l=>l.quaternion.copy(camera.quaternion));camera.updateProjectionMatrix();renderer.render(scene,camera);
  }
  render(0);
  function dispose(){observer?.disconnect();for(const g of geometries)g.dispose();for(const m of materials)m.dispose();for(const t of textures)t.dispose();renderer.dispose();renderer.domElement.remove();}
  return {render,resize,dispose};
}
