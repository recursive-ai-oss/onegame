#!/usr/bin/env python3
"""A few App Store Connect API calls for putting a development build on a device without opening Xcode.

  asc.py devices                                  list registered devices
  asc.py register UDID NAME [ios|macos]           register a device (no-op when the UDID is already there)
  asc.py certificates                             list development certificates
  asc.py profiles                                 list provisioning profiles
  asc.py certificate CSR.pem OUT.cer              make a development certificate from a certificate signing request (DER file out)
  asc.py profile BUNDLE_ID OUT.mobileprovision    make sure the bundle id exists, then write a development profile that
                                                  lists every valid development certificate and every registered iOS device

Credentials come from the environment and never from the repository: ASC_KEY_FILE (the .p8), ASC_KEY_ID, ASC_ISSUER_ID.
Output is JSON. The API is documented at https://developer.apple.com/documentation/appstoreconnectapi.
"""
import base64, json, os, sys, time, urllib.error, urllib.request

from cryptography.hazmat.primitives import hashes, serialization
from cryptography.hazmat.primitives.asymmetric import ec, utils

BASE = 'https://api.appstoreconnect.apple.com/v1'


def b64(b):
    return base64.urlsafe_b64encode(b).rstrip(b'=').decode()


def token():
    key = serialization.load_pem_private_key(open(os.environ['ASC_KEY_FILE'], 'rb').read(), None)
    head = b64(json.dumps({'alg': 'ES256', 'kid': os.environ['ASC_KEY_ID'], 'typ': 'JWT'}).encode())
    body = b64(json.dumps({'iss': os.environ['ASC_ISSUER_ID'], 'iat': int(time.time()), 'exp': int(time.time()) + 600, 'aud': 'appstoreconnect-v1'}).encode())
    der = key.sign(f'{head}.{body}'.encode(), ec.ECDSA(hashes.SHA256()))
    r, s = utils.decode_dss_signature(der)
    return f'{head}.{body}.{b64(r.to_bytes(32, "big") + s.to_bytes(32, "big"))}'


def api(method, path, body=None):
    req = urllib.request.Request(BASE + path if path.startswith('/') else path, method=method,
                                 data=json.dumps(body).encode() if body is not None else None,
                                 headers={'Authorization': 'Bearer ' + token(), 'Content-Type': 'application/json'})
    try:
        with urllib.request.urlopen(req, timeout=60) as r:
            raw = r.read()
            return json.loads(raw) if raw else {}
    except urllib.error.HTTPError as e:
        raise SystemExit(json.dumps({'ok': False, 'status': e.code, 'error': json.loads(e.read() or b'{}')}))


def all_pages(path):
    out, url = [], path
    while url:
        d = api('GET', url)
        out += d.get('data', [])
        url = d.get('links', {}).get('next')
    return out


def devices():
    return [{'id': d['id'], **{k: d['attributes'].get(k) for k in ('name', 'udid', 'platform', 'status', 'deviceClass', 'model')}} for d in all_pages('/devices?limit=200')]


def certificates():
    return [{'id': c['id'], **{k: c['attributes'].get(k) for k in ('name', 'certificateType', 'displayName', 'expirationDate', 'serialNumber')}} for c in all_pages('/certificates?limit=200')]


def main():
    cmd = sys.argv[1] if len(sys.argv) > 1 else ''
    if cmd == 'devices':
        print(json.dumps({'ok': True, 'devices': devices()}))
    elif cmd == 'certificates':
        print(json.dumps({'ok': True, 'certificates': certificates()}))
    elif cmd == 'profiles':
        ps = all_pages('/profiles?limit=200')
        print(json.dumps({'ok': True, 'profiles': [{'id': p['id'], **{k: p['attributes'].get(k) for k in ('name', 'profileType', 'profileState', 'expirationDate')}} for p in ps]}))
    elif cmd == 'certificate':
        csr = open(sys.argv[2]).read()
        c = api('POST', '/certificates', {'data': {'type': 'certificates', 'attributes': {'certificateType': 'DEVELOPMENT', 'csrContent': csr}}})
        open(sys.argv[3], 'wb').write(base64.b64decode(c['data']['attributes']['certificateContent']))
        print(json.dumps({'ok': True, 'id': c['data']['id'], 'expires': c['data']['attributes'].get('expirationDate')}))
    elif cmd == 'register':
        udid, name = sys.argv[2], sys.argv[3]
        platform = {'ios': 'IOS', 'macos': 'MAC_OS'}[sys.argv[4] if len(sys.argv) > 4 else 'ios']
        have = [d for d in devices() if d['udid'].replace('-', '').lower() == udid.replace('-', '').lower()]
        if have:
            print(json.dumps({'ok': True, 'registered': False, 'device': have[0]}))
        else:
            d = api('POST', '/devices', {'data': {'type': 'devices', 'attributes': {'name': name, 'udid': udid, 'platform': platform}}})
            print(json.dumps({'ok': True, 'registered': True, 'id': d['data']['id']}))
    elif cmd == 'profile':
        bundle, out = sys.argv[2], sys.argv[3]
        ids = [b for b in all_pages('/bundleIds?limit=200') if b['attributes']['identifier'] == bundle]
        if ids:
            bid = ids[0]['id']
        else:
            bid = api('POST', '/bundleIds', {'data': {'type': 'bundleIds', 'attributes': {'identifier': bundle, 'name': 'onegame ' + bundle.split('.')[-1], 'platform': 'IOS'}}})['data']['id']
        certs = [c['id'] for c in certificates() if c['certificateType'] in ('IOS_DEVELOPMENT', 'DEVELOPMENT') and c['expirationDate'] > time.strftime('%Y-%m-%dT%H:%M')]
        devs = [d['id'] for d in devices() if d['platform'] == 'IOS' and d['status'] == 'ENABLED']
        if not certs or not devs:
            raise SystemExit(json.dumps({'ok': False, 'error': f'need a development certificate and a device (have {len(certs)} certificates, {len(devs)} devices)'}))
        name = f'onegame dev {bundle} {int(time.time())}'
        p = api('POST', '/profiles', {'data': {'type': 'profiles', 'attributes': {'name': name, 'profileType': 'IOS_APP_DEVELOPMENT'},
                                               'relationships': {'bundleId': {'data': {'type': 'bundleIds', 'id': bid}},
                                                                 'certificates': {'data': [{'type': 'certificates', 'id': c} for c in certs]},
                                                                 'devices': {'data': [{'type': 'devices', 'id': d} for d in devs]}}}})
        open(out, 'wb').write(base64.b64decode(p['data']['attributes']['profileContent']))
        print(json.dumps({'ok': True, 'profile': out, 'name': name, 'devices': len(devs), 'certificates': len(certs)}))
    else:
        print(__doc__)
        sys.exit(2)


main()
