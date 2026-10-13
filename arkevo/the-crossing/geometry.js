// Executable geometry for the browser-local challenge; independent of CS2 physics.
export const route=[{x:105,y:470},{x:290,y:445},{x:470,y:360},{x:625,y:300},{x:860,y:170}];
export const observers=[{x:365,y:108},{x:800,y:470}];
export const walls=[{x:65,y:55,w:185,h:275},{x:470,y:75,w:240,h:155},{x:78,y:535,w:425,h:40},{x:748,y:280,w:190,h:95},{x:880,y:46,w:58,h:165}];
export const smokeRadius=100;
export function intersects(a,b,c,r=smokeRadius){let vx=b.x-a.x,vy=b.y-a.y;let u=Math.max(0,Math.min(1,((c.x-a.x)*vx+(c.y-a.y)*vy)/(vx*vx+vy*vy)));return Math.hypot(a.x+u*vx-c.x,a.y+u*vy-c.y)<=r}
export function inWall(p){return walls.some(w=>p.x>=w.x&&p.x<=w.x+w.w&&p.y>=w.y&&p.y<=w.y+w.h)}
export function segRect(a,b,r){let min=0,max=1,dx=b.x-a.x,dy=b.y-a.y;for(const [p,q] of [[-dx,a.x-r.x],[dx,r.x+r.w-a.x],[-dy,a.y-r.y],[dy,r.y+r.h-a.y]]){if(p===0){if(q<0)return false}else{let t=q/p;if(p<0){if(t>max)return false;min=Math.max(min,t)}else{if(t<min)return false;max=Math.min(max,t)}}}return true}
export function pointAt(u){let q=Math.min(.999999,u)*(route.length-1),i=Math.floor(q),v=q-i;return{x:route[i].x+(route[i+1].x-route[i].x)*v,y:route[i].y+(route[i+1].y-route[i].y)*v}}
export function visible(a,b){return !walls.some(w=>segRect(a,b,w))}
export function evaluate(smokes){let count=0,total=0,exposed=[];for(let i=0;i<=100;i++){const p=pointAt(i/100),threats=observers.filter(o=>visible(o,p));if(!threats.length)continue;total++;if(threats.every(o=>smokes.some(c=>intersects(o,p,c))))count++;else exposed.push(p)}return{ratio:total?Math.round(count/total*100):100,exposed,total,count}}
function bits(n){n=n-((n>>>1)&0x55555555);n=(n&0x33333333)+((n>>>2)&0x33333333);return (((n+(n>>>4))&0x0f0f0f0f)*0x01010101)>>>24}
export function solve(){
 const rays=[];for(let i=0;i<=100;i++){const p=pointAt(i/100);for(const o of observers)if(visible(o,p))rays.push([o,p]);}
 const candidates=[];for(let y=70;y<550;y+=25)for(let x=60;x<950;x+=25){let c={x,y};if(inWall(c)||observers.some(o=>Math.hypot(x-o.x,y-o.y)<115))continue;const mask=new Uint32Array(Math.ceil(rays.length/32));rays.forEach(([o,p],i)=>{if(intersects(o,p,c))mask[i>>>5]|=1<<(i%32)});candidates.push({c,mask});}
 let best=-1,chosen=null;outer:for(let i=0;i<candidates.length;i++){for(let j=i+1;j<candidates.length;j++){let score=0;for(let k=0;k<candidates[i].mask.length;k++)score+=bits(candidates[i].mask[k]|candidates[j].mask[k]);if(score>best){best=score;chosen=[candidates[i].c,candidates[j].c];if(score===rays.length)break outer;}}}
 return{smokes:chosen,rays:rays.length,blocked:best,candidates:candidates.length,result:evaluate(chosen)};
}
