"""Lee el índice de un ZIP remoto por rangos HTTP y extrae solo las entradas pedidas.
Uso:
  python zip_parcial.py lista <url>                      -> nombre y tamaño de cada entrada
  python zip_parcial.py extrae <url> <destino> <prefijo>... -> extrae las entradas que empiezan por algún prefijo
"""
import struct, sys, urllib.request, zlib, os


def get(url, start, end):
    import time
    for attempt in range(6):
        try:
            req = urllib.request.Request(url, headers={'Range': f'bytes={start}-{end}'})
            with urllib.request.urlopen(req, timeout=180) as r:
                data = r.read()
            if len(data) == end - start + 1:
                return data
        except Exception as e:
            print('  reintento', attempt + 1, type(e).__name__)
        time.sleep(3 * (attempt + 1))
    raise IOError(f'No se pudo descargar el rango {start}-{end}')


def size(url):
    req = urllib.request.Request(url, method='HEAD')
    with urllib.request.urlopen(req, timeout=60) as r:
        return int(r.headers['Content-Length'])


def central(url):
    n = size(url)
    tail = get(url, max(0, n - 66000), n - 1)
    i = tail.rfind(b'PK\x05\x06')
    cd_size, cd_off = struct.unpack('<II', tail[i + 12:i + 20])
    if cd_off == 0xFFFFFFFF:  # ZIP64
        j = tail.rfind(b'PK\x06\x06')
        cd_size, cd_off = struct.unpack('<QQ', tail[j + 40:j + 56])
    cd = get(url, cd_off, cd_off + cd_size - 1)
    entries, p = [], 0
    while p < len(cd) and cd[p:p + 4] == b'PK\x01\x02':
        method, = struct.unpack('<H', cd[p + 10:p + 12])
        csize, usize = struct.unpack('<II', cd[p + 20:p + 28])
        nlen, elen, clen = struct.unpack('<HHH', cd[p + 28:p + 34])
        loff, = struct.unpack('<I', cd[p + 42:p + 46])
        name = cd[p + 46:p + 46 + nlen].decode('utf-8', 'replace')
        extra = cd[p + 46 + nlen:p + 46 + nlen + elen]
        q = 0
        while q + 4 <= len(extra):  # campos ZIP64
            hid, hl = struct.unpack('<HH', extra[q:q + 4])
            if hid == 1:
                vals = extra[q + 4:q + 4 + hl]; k = 0
                if usize == 0xFFFFFFFF: usize, = struct.unpack('<Q', vals[k:k + 8]); k += 8
                if csize == 0xFFFFFFFF: csize, = struct.unpack('<Q', vals[k:k + 8]); k += 8
                if loff == 0xFFFFFFFF: loff, = struct.unpack('<Q', vals[k:k + 8]); k += 8
            q += 4 + hl
        entries.append((name, method, csize, usize, loff))
        p += 46 + nlen + elen + clen
    return entries


def extract(url, entry, dest):
    name, method, csize, usize, loff = entry
    if name.endswith('/'):
        return
    head = get(url, loff, loff + 29)
    nlen, elen = struct.unpack('<HH', head[26:30])
    start = loff + 30 + nlen + elen
    data = get(url, start, start + csize - 1) if csize else b''
    if method == 8:
        data = zlib.decompress(data, -15)
    elif method != 0:
        raise ValueError(f'Compresión no soportada {method} en {name}')
    out = os.path.join(dest, *name.split('/'))
    os.makedirs(os.path.dirname(out), exist_ok=True)
    open(out, 'wb').write(data)


if __name__ == '__main__':
    cmd, url = sys.argv[1], sys.argv[2]
    ents = central(url)
    if cmd == 'lista':
        for e in ents:
            if not e[0].endswith('/'): print(f'{e[3]:>10} {e[0]}')
    else:
        dest, prefixes = sys.argv[3], sys.argv[4:]
        sel = [e for e in ents if any(e[0].startswith(p) for p in prefixes)]
        total = sum(e[2] for e in sel)
        print(f'{len(sel)} entradas, {total / 1048576:.1f} MB comprimidos')
        for e in sel:
            extract(url, e, dest)
        print('ok')
