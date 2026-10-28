"""One-process, localhost-only Web preview with an explicit expiry (default 15 min)."""
import argparse,http.server,json,pathlib,time,os,signal
ap=argparse.ArgumentParser();ap.add_argument('--port',type=int,default=0);ap.add_argument('--seconds',type=int,default=900)
ap.add_argument('--root',type=pathlib.Path,default=pathlib.Path(__file__).resolve().parents[1]/'web')
args=ap.parse_args()
if not 1<=args.seconds<=7200:ap.error('Lifetime must be 1–7200 seconds')
root=args.root.resolve()
if not root.is_dir():ap.error('Preview root must be an existing directory')
class Handler(http.server.SimpleHTTPRequestHandler):
 extensions_map={**http.server.SimpleHTTPRequestHandler.extensions_map,'.wasm':'application/wasm','.pck':'application/octet-stream','.js':'text/javascript'}
 def __init__(self,*a,**kw):super().__init__(*a,directory=str(root),**kw)
 def end_headers(self):
  self.send_header('Cache-Control','no-store');super().end_headers()
 def copyfile(self,source,outputfile):
  self.connection.settimeout(min(30.0,max(0.05,self.server.deadline-time.monotonic())))
  while time.monotonic()<self.server.deadline:
   chunk=source.read(65536)
   if not chunk:break
   outputfile.write(chunk)
class PreviewServer(http.server.HTTPServer):
 def get_request(self):
  request,address=super().get_request()
  request.settimeout(min(1.0,max(0.05,self.deadline-time.monotonic())))
  return request,address
server=PreviewServer(('127.0.0.1',args.port),Handler)
server.timeout=1
print(json.dumps({'url':f'http://127.0.0.1:{server.server_port}/','pid':os.getpid(),'expires_seconds':args.seconds,'process_limit':1}),flush=True)
deadline=time.monotonic()+args.seconds
server.deadline=deadline
def stop(signum,frame):
 raise SystemExit(0)
signal.signal(signal.SIGTERM,stop)
if hasattr(signal,'setitimer'):
 signal.signal(signal.SIGALRM,stop)
 signal.setitimer(signal.ITIMER_REAL,args.seconds)
try:
 while time.monotonic()<deadline:server.handle_request()
finally:
 if hasattr(signal,'setitimer'):signal.setitimer(signal.ITIMER_REAL,0)
 server.server_close()
