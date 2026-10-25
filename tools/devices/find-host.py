#!/usr/bin/env python3
"""Finds a Linux handheld (a Steam Deck) on the local network with no help from anyone and prints its address.

  find-host.py [--user deck] [--name steamdeck] [--ssh-key FILE]

Tries, in order: $DECK_HOST; the multicast-DNS name <name>.local; every address of every local /24 that answers on port 22, where
it logs in with the key (no password) as the user and accepts the first whose /etc/os-release says SteamOS (or ID=steamos). Prints
the address on stdout and how it was found on stderr; exit 1 when nothing answers."""
import argparse, concurrent.futures as cf, ipaddress, os, socket, struct, subprocess, sys, time

ap = argparse.ArgumentParser()
ap.add_argument('--user', default=os.environ.get('DECK_USER', 'deck'))
ap.add_argument('--name', default='steamdeck')
ap.add_argument('--ssh-key')
args = ap.parse_args()
SSH = ['ssh', '-o', 'BatchMode=yes', '-o', 'ConnectTimeout=4', '-o', 'StrictHostKeyChecking=accept-new'] + (['-i', args.ssh_key] if args.ssh_key else [])


def is_deck(host):
    p = subprocess.run(SSH + [f'{args.user}@{host}', 'cat /etc/os-release'], capture_output=True, text=True, timeout=15)
    return p.returncode == 0 and ('steamos' in p.stdout.lower())


def mdns(name):
    """One multicast-DNS A query (RFC 6762 section 5.1); returns the first IPv4 answer."""
    q = struct.pack('>HHHHHH', 0, 0, 1, 0, 0, 0) + b''.join(bytes([len(l)]) + l.encode() for l in (name + '.local').split('.')) + b'\0' + struct.pack('>HH', 1, 0x8001)
    s = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    s.settimeout(1.5)
    try:
        for _ in range(2):
            s.sendto(q, ('224.0.0.251', 5353))
            end = time.time() + 1.5
            while time.time() < end:
                try:
                    data, _ = s.recvfrom(2048)
                except socket.timeout:
                    break
                if len(data) > 12 and data[6:8] != b'\0\0':     # has answers; the A record's data is the last four bytes of its RR
                    for rr in (r for r in range(12, len(data) - 14) if data[r:r + 2] == b'\x00\x01' and data[r + 8:r + 10] == b'\x00\x04'):
                        ip = socket.inet_ntoa(data[rr + 10:rr + 14])   # type A (1), class IN, ttl, rdlength 4, the address
                        if not ip.startswith('0.'):
                            return ip
    finally:
        s.close()
    return None


def open22(ip):
    try:
        with socket.create_connection((ip, 22), timeout=0.6):
            return ip
    except OSError:
        return None


def local_networks():
    out = subprocess.run(['ip', '-4', '-o', 'addr', 'show'], capture_output=True, text=True).stdout if sys.platform.startswith('linux') else \
        subprocess.run(['ifconfig'], capture_output=True, text=True).stdout
    nets = set()
    for line in out.splitlines():
        for tok in line.split():
            if '/' in tok and tok[0].isdigit():
                try:
                    n = ipaddress.ip_interface(tok)
                    if not n.ip.is_loopback and n.network.num_addresses <= 1024 and not str(n.ip).startswith(('172.17.', '172.18.')):
                        nets.add(ipaddress.ip_network(f'{n.ip}/24', strict=False))
                except ValueError:
                    pass
    return sorted(nets)


def main():
    if os.environ.get('DECK_HOST'):
        print('DECK_HOST', file=sys.stderr); print(os.environ['DECK_HOST']); return 0
    ip = mdns(args.name)
    if ip and is_deck(ip):
        print(f'found by mDNS ({args.name}.local)', file=sys.stderr); print(ip); return 0
    for net in local_networks():
        with cf.ThreadPoolExecutor(64) as ex:
            hosts = [h for h in ex.map(open22, (str(a) for a in net.hosts())) if h]
        for h in hosts:
            try:
                if is_deck(h):
                    print(f'found by scanning {net} for port 22 ({len(hosts)} answered)', file=sys.stderr); print(h); return 0
            except subprocess.TimeoutExpired:
                pass
        print(f'scanned {net}: {len(hosts)} hosts with port 22, none a SteamOS machine for user {args.user}', file=sys.stderr)
    return 1


sys.exit(main())
