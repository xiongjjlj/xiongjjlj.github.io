import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import vm from 'node:vm';
import { test } from 'node:test';
const text = await readFile(process.argv[2], 'utf8');
const start = text.indexOf('const Preloader =');
const end = text.indexOf('\n/**', start);
const source = text.slice(start, end) + '\nthis.Preloader = Preloader;';
function context(fetch, timers = {}) {
  const active = new Set();
  const c = { fetch, Response, ReadableStream, AbortController, ArrayBuffer, Uint8Array,
    setTimeout(fn, delay) { const id=setTimeout(()=>{ active.delete(id); fn(); }, delay >= 30000 ? (timers.timeout ?? 1000) : 1); active.add(id);return id; },
    clearTimeout(id) { active.delete(id);clearTimeout(id); }, requestAnimationFrame() {} };
  vm.runInNewContext(source,c);return {loader:new c.Preloader(),active};
}
function broken() { return new Response(new ReadableStream({ pull(c) { c.error(new TypeError('interrupted')); } })); }
for (const raw of [false,true]) {
  test(`interrupted ${raw?'WASM':'PCK'} body retries and succeeds`, {timeout:2000}, async()=>{
    let calls=0;const {loader,active}=context(()=>Promise.resolve(++calls===1?broken():new Response(new Uint8Array([1,2,3]),{headers:{'Content-Type':'application/wasm'}})));
    const result=await loader.loadPromise('fixture',3,raw);assert.deepEqual([...new Uint8Array(raw?await result.arrayBuffer():result)],[1,2,3]);assert.equal(calls,2);assert.equal(active.size,0);
    if(raw)assert.equal(result.headers.get('Content-Type'),'application/wasm');
  });
}
test('repeated body failures reject after four attempts and stop progress', {timeout:2000}, async()=>{
 let calls=0;const {loader,active}=context(()=>{calls++;return Promise.resolve(broken());});
 await assert.rejects(loader.loadPromise('fixture',3),/after 4 attempts.*reload/);assert.equal(calls,4);assert.equal(active.size,0);
});
test('stalled body is aborted and retries remain bounded', {timeout:2000}, async()=>{
 let calls=0,aborts=0;
 const {loader,active}=context((url,{signal})=>{calls++;return Promise.resolve(new Response(new ReadableStream({start(c){signal.addEventListener('abort',()=>{aborts++;c.error(signal.reason);},{once:true});}})));},{timeout:10});
 await assert.rejects(loader.loadPromise('fixture',3,true),/after 4 attempts/);assert.equal(calls,4);assert.equal(aborts,4);assert.equal(active.size,0);
});
test('HTTP and fetch rejection finish with no active timers', {timeout:2000}, async()=>{
 for(const fetch of [()=>Promise.resolve(new Response('missing',{status:404})),()=>Promise.reject(new TypeError('offline'))]){
 const {loader,active}=context(fetch);await assert.rejects(loader.loadPromise('fixture',3),/after 4 attempts/);assert.equal(active.size,0);
 }
});
