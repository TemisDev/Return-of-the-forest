import sys,os; sys.path.insert(0,os.path.dirname(__file__)); from common import *
import numpy as np
a=sys.argv[sys.argv.index('--')+1:]
def L(f):
    im=load(f,True); im.scale(960,540); x=np.empty(960*540*4,np.float32); im.pixels.foreach_get(x); return x.reshape(540,960,4)
top=np.concatenate([L(a[0]),np.ones((540,8,4),np.float32),L(a[1])],1)
bot=np.concatenate([L(a[2]),np.ones((540,8,4),np.float32),L(a[3])],1)
save(np.concatenate([bot,np.ones((8,1928,4),np.float32),top],0),a[4])
