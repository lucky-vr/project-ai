from PIL import Image
from pathlib import Path
import numpy as np
from skimage.color import rgb2lab
import json,itertools
import os
root=Path(__file__).resolve().parent
appearance_dir=Path(os.environ.get('VILLAGE_AUDIT_ROOT','/tmp/village_model_audit'))/'prop_appearance'
views=('front','top','three_quarter')
m=json.load(open(root/'asset_manifest.json'))
assets=[a for v in m['venues'].values() for a in v['assets'][1:]]
D={}
for a in assets:
 name=a['name'];D[name]={}
 for view in views:
  im=np.asarray(Image.open(appearance_dir/f'{name}_{view}.png').convert('RGBA'),dtype=np.uint8)
  mask=im[:,:,3]>128
  lab=rgb2lab(im[:,:,:3]/255.)
  D[name][view]=(mask,lab)
R=[]
for a,b in itertools.combinations(assets,2):
 n1,n2=a['name'],b['name'];parts=[]
 for view in views:
  ma,la=D[n1][view];mb,lb=D[n2][view]
  inter=ma&mb;un=ma|mb
  shape=inter.sum()/un.sum()
  if inter.any():
   col=np.linalg.norm(la[inter]-lb[inter],axis=1)
   color=float(np.median(col))
  else:color=100.
  parts.append((shape,color))
 score=sum(x[0] for x in parts)/len(parts)
 if score>.6:
  color=sum(x[1] for x in parts)/len(parts)
  R.append({'a':n1,'b':n2,'role_a':a['role'],'role_b':b['role'],'shape':round(score,3),'median_lab_delta':round(color,1),'front_shape':round(parts[0][0],3),'top_shape':round(parts[1][0],3),'three_quarter_shape':round(parts[2][0],3)})
R.sort(key=lambda r:(-r['shape'],r['median_lab_delta']))
print('shape candidates',len(R),'color<10',sum(r['median_lab_delta']<10 for r in R),'color<20',sum(r['median_lab_delta']<20 for r in R),'color<30',sum(r['median_lab_delta']<30 for r in R))
print('worst shape')
for r in R[:30]:print(r)
print('low color delta of high-shape')
for r in sorted(R,key=lambda q:(q['median_lab_delta'],-q['shape']))[:40]:print(r)
(root/'prop_appearance_audit.json').write_text(json.dumps({'method':'Front/top/three-quarter silhouette IoU plus median overlapping CIELAB DeltaE76 from Blender Workbench material-color renders','pairs_shape_above_0_60':len(R),'pairs_shape_above_0_60_and_lab_delta_below_20':sum(r['median_lab_delta']<20 for r in R),'pairs':R},indent=2)+'\n')
