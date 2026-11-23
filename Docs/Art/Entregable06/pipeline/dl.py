import json,os,hashlib,urllib.request,sys,concurrent.futures as cf
sys.stdout.reconfigure(encoding='utf-8')
plan={'rock_moss_set_01':'2k','rock_moss_set_02':'2k','namaqualand_boulder_02':'2k','boulder_01':'2k','dead_tree_trunk_02':'2k','dry_branches_medium_01':'2k','tree_stump_01':'2k','fern_02':'1k','grass_medium_01':'1k','grass_medium_02':'1k','shrub_04':'1k','moss_01':'1k','celandine_01':'1k','dandelion_01':'1k','tree_small_02':'1k','island_tree_02':'1k','root_cluster_01':'1k','pine_roots':'1k'}
jobs=[]
for f in os.listdir('files'):
    id=f[:-5]; d=json.load(open('files/'+f))
    if 'fbx' in d and id not in plan: continue
    if 'fbx' in d:
        e=d['fbx'][plan[id]]['fbx']; base=f'raw/models/{id}'
        jobs.append((e['url'],e['md5'],f'{base}/{id}.fbx'))
        for rel,v in e['include'].items(): jobs.append((v['url'],v['md5'],f'{base}/{rel}'))
    elif 'hdri' in d:
        e=d['hdri']['2k']['hdr']; jobs.append((e['url'],e['md5'],f'raw/hdri/{id}_2k.hdr'))
    else:
        for m in ['Diffuse','nor_gl','Rough','AO','Displacement']:
            if m in d:
                e=d[m]['2k']['jpg']; jobs.append((e['url'],e['md5'],f'raw/textures/{id}/{os.path.basename(e["url"])}'))
def get(j):
    url,md5,path=j
    os.makedirs(os.path.dirname(path),exist_ok=True)
    if os.path.exists(path) and hashlib.md5(open(path,'rb').read()).hexdigest()==md5: return 'skip'
    req=urllib.request.Request(url,headers={'User-Agent':'asset-download-client'})
    data=urllib.request.urlopen(req,timeout=300).read()
    ok=hashlib.md5(data).hexdigest()==md5
    open(path,'wb').write(data)
    return 'ok' if ok else 'MD5FAIL '+path
with cf.ThreadPoolExecutor(6) as ex:
    res=list(ex.map(get,jobs))
print(len(jobs),'files', {r:res.count(r) for r in set(res)})
