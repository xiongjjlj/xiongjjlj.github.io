// Original training-range geometry. Units are metres; x/z is the ground plane.
// Visibility is a conservative 2D model of opaque building footprints and smoke
// cylinders. Small props, animation, aiming accuracy and game-engine physics are
// intentionally excluded. This is a mechanism demonstration, not model telemetry.
export const ROUTE = Object.freeze([[-10,2.25],[-6.5,2.2],[-3.4,1.2],[-.8,.25],[1.2,-1.4],[2,-3.8],[2,-6.1],[2,-9.1]]);
export const OBSERVERS = Object.freeze([{id:'A',x:8.4,z:2},{id:'B',x:10.4,z:3.35},{id:'C',x:6.8,z:-3.4}]);
export const SMOKES = Object.freeze([{id:'S1',x:1.3,z:.4,radius:2.6},{id:'S2',x:3.3,z:-3.6,radius:2.45}]);
const PREP_Q=1/(ROUTE.length-1);
export const EXIT = Object.freeze({x:2,z:-9.1,radius:.65});
export const WALLS = Object.freeze([
  {id:'west',minX:-13.1,maxX:-6.9,minZ:-9.9,maxZ:-4.1},
  {id:'north-west',minX:-6.4,maxX:.4,minZ:-9.95,maxZ:-7.45},
  {id:'north-east',minX:4.1,maxX:13.1,minZ:-10.4,maxZ:-5.6},
  {id:'south-west',minX:-13.6,maxX:-8.4,minZ:5.15,maxZ:10.85},
  {id:'south-east',minX:7.5,maxX:13.7,minZ:6.3,maxZ:10.7},
  {id:'gate-left',minX:-.675,maxX:.675,minZ:-7.875,maxZ:-6.625},
  {id:'gate-right',minX:3.325,maxX:4.675,minZ:-7.875,maxZ:-6.625}
]);
const clamp=(n,a=0,b=1)=>Math.max(a,Math.min(b,n));
const smooth=(a,b,t)=>{const x=clamp((t-a)/(b-a));return x*x*(3-2*x);};
export function routeAt(q){const s=clamp(q)*(ROUTE.length-1),i=Math.min(ROUTE.length-2,Math.floor(s)),u=s-i;return {x:ROUTE[i][0]+u*(ROUTE[i+1][0]-ROUTE[i][0]),y:0,z:ROUTE[i][1]+u*(ROUTE[i+1][1]-ROUTE[i][1])};}
export function segmentCircle(a,b,c){const dx=b.x-a.x,dz=b.z-a.z,fx=a.x-c.x,fz=a.z-c.z,aa=dx*dx+dz*dz,bb=2*(fx*dx+fz*dz),cc=fx*fx+fz*fz-c.radius*c.radius;if(aa<1e-12)return cc<=0?0:null;const det=bb*bb-4*aa*cc;if(det<0)return null;const near=(-bb-Math.sqrt(det))/(2*aa),far=(-bb+Math.sqrt(det))/(2*aa);return far>=0&&near<=1?Math.max(0,near):null;}
export function segmentRectangle(a,b,r){let lo=0,hi=1;for(const [axis,mn,mx] of [['x',r.minX,r.maxX],['z',r.minZ,r.maxZ]]){const d=b[axis]-a[axis];if(Math.abs(d)<1e-10){if(a[axis]<mn||a[axis]>mx)return null;}else{let p=(mn-a[axis])/d,q=(mx-a[axis])/d;if(p>q)[p,q]=[q,p];lo=Math.max(lo,p);hi=Math.min(hi,q);if(lo>hi)return null;}}return lo;}
export function visibilityAt(position,smokes=[]){return OBSERVERS.map(observer=>{let hit=null;for(const w of WALLS){const u=segmentRectangle(observer,position,w);if(u!==null&&(!hit||u<hit.u))hit={u,status:'wall',obstacleId:w.id};}for(const smoke of smokes){if(smoke.radius<=0)continue;const u=segmentCircle(observer,position,smoke);if(u!==null&&(!hit||u<hit.u))hit={u,status:'smoke',obstacleId:smoke.id};}const u=hit?.u??1;return {observerId:observer.id,status:hit?.status??'exposed',obstacleId:hit?.obstacleId??null,from:{x:observer.x,y:1.15,z:observer.z},target:{x:position.x,y:.98,z:position.z},end:{x:observer.x+u*(position.x-observer.x),y:1.15+u*(.98-1.15),z:observer.z+u*(position.z-observer.z)},fraction:u};});}
export function sceneState(time){const t=clamp(Number(time)||0,0,84);let q=0;
 if(t<8.5)q=smooth(.4,6.7,t)*.43;else if(t<18)q=smooth(8.7,15.3,t)*.60;else if(t<40)q=PREP_Q;else if(t<50)q=PREP_Q+smooth(40,48,t)*(.632-PREP_Q);else if(t<64)q=PREP_Q;else q=PREP_Q+smooth(64,78,t)*(1-PREP_Q);
 const strengths=[t>=34?(t<50?smooth(34,38,t):smooth(52,59,t)):0,smooth(58,63,t)];
 const smokes=SMOKES.map((s,i)=>({...s,strength:strengths[i],radius:s.radius*strengths[i]}));
 const position=routeAt(q),rays=visibilityAt(position,smokes),exposedCount=rays.filter(r=>r.status==='exposed').length,smokeBlockedCount=rays.filter(r=>r.status==='smoke').length,wallBlockedCount=rays.filter(r=>r.status==='wall').length;
 const failed=t>=6.7&&t<8.5||t>=15.3&&t<18||t>=48&&t<50;
 const walking=t>.4&&t<6.7||t>=8.7&&t<15.3||t>=40&&t<48||t>=64&&t<78;
 return {t,q,position,smokes,rays,exposedCount,smokeBlockedCount,wallBlockedCount,blockedCount:smokeBlockedCount+wallBlockedCount,reached:t>=78&&Math.hypot(position.x-EXIT.x,position.z-EXIT.z)<EXIT.radius,failed,walking,attempt:t<8.5?1:t<18?2:t<40?0:t<50?3:t<64?0:4,exit:EXIT};
}
export function validateLayout(sampleCount=401){const variants={baseline:[],v1:[SMOKES[0]],v2:SMOKES},result={sampleCount,raysPerSample:OBSERVERS.length,totalRays:sampleCount*OBSERVERS.length,routeClear:true,variants:{}};
 for(let i=0;i<sampleCount;i++){const p=routeAt(i/(sampleCount-1));if(WALLS.some(w=>p.x>=w.minX-.22&&p.x<=w.maxX+.22&&p.z>=w.minZ-.22&&p.z<=w.maxZ+.22))result.routeClear=false;}
 for(const [name,smokes] of Object.entries(variants)){let exposedSamples=0,exposedRays=0,smokeBlockedRays=0,wallBlockedRays=0,maxExposed=0;for(let i=0;i<sampleCount;i++){const rays=visibilityAt(routeAt(i/(sampleCount-1)),smokes),n=rays.filter(r=>r.status==='exposed').length;exposedSamples+=n>0?1:0;exposedRays+=n;maxExposed=Math.max(maxExposed,n);smokeBlockedRays+=rays.filter(r=>r.status==='smoke').length;wallBlockedRays+=rays.filter(r=>r.status==='wall').length;}result.variants[name]={exposedSamples,exposedRays,smokeBlockedRays,wallBlockedRays,maxExposed,safeSamples:sampleCount-exposedSamples};}
 let timeExposure=0;for(let i=0;i<=1400;i++)timeExposure+=sceneState(64+i/100).exposedCount;
 result.v2TimeSamples=1401;result.v2TimeExposedRays=timeExposure;result.v1Failure=visibilityAt(routeAt(.632),[SMOKES[0]]).map(r=>({observerId:r.observerId,status:r.status,obstacleId:r.obstacleId}));result.arrival=sceneState(78).reached;return result;
}
export const VALIDATION = validateLayout();
