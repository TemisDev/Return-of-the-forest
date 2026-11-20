import sys,glob,os; sys.path.insert(0,os.path.dirname(__file__)); from common import *
import numpy as np
D=sys.argv[sys.argv.index('--')+1]; out=sys.argv[sys.argv.index('--')+2]; views=sys.argv[sys.argv.index('--')+3].split(',')
rows=[]
for v in views:
    cells=[]
    for st in ['degradado','restaurado']:
        f=glob.glob(os.path.join(D,f'v6_{st}_{v}*.png'))[0]
        im=load(f,True); im.scale(800,450); a=np.empty(800*450*4,np.float32); im.pixels.foreach_get(a); cells.append(a.reshape(450,800,4))
    rows.append(np.concatenate([cells[0],np.ones((450,8,4),np.float32),cells[1]],1))
save(np.concatenate(rows[::-1],0),out)
