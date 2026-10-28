"""Keep the recovered motion/transition method bodies unchanged for native replay."""
import hashlib,json,re
from pathlib import Path
root=Path(__file__).resolve().parents[2]
source=(root/'recovered-source/MovingFloorReactive.cs').read_text()
parts=[]; hashes={}
for name,signature in [('FixedUpdate','protected override void FixedUpdate()'),('ToggleTargetPosition','private void ToggleTargetPosition(Actor activator = null, bool springBack = false)')]:
 start=source.index('\t'+signature);p=source.index('{',start)+1;depth=1
 while depth:depth+=(source[p]=='{')-(source[p]=='}');p+=1
 body=source[start:p];parts.append(body);hashes[name]=hashlib.sha256(body.encode()).hexdigest()
(root/'tools/reactive-floor-native/ExtractedReference.cs').write_text('using UnityEngine;\npublic partial class ReactiveReference : MovingFloor {\n'+'\n'.join(parts)+'\n}\n')
(root/'evidence/reactive-floor-extracted-methods.json').write_text(json.dumps({'source':'recovered-source/MovingFloorReactive.cs','sha256':hashes,'body_edits':False,'scope':'Exact recovered FixedUpdate and ToggleTargetPosition bodies; native Unity transforms/math with explicit clock, navigation, camera and flash collaborators.'},indent=2)+'\n')
