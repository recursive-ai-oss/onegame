# Crypto test vectors

Published test data for the primitives in `src/OneGame.Core/Crypto`. Used by tests only.

- `wycheproof-x25519.json`, `wycheproof-chacha20_poly1305.json`: Project Wycheproof (Google, Apache-2.0), `testvectors_v1/x25519_test.json` and `chacha20_poly1305_test.json`, unchanged. SHA-256 `35c3f5231cf25cc640b524d403461deee9e49441d5d915a3a25b2c8ff5adbe7d` and `fe61d25f90e1bde4461d00eafe61049e5f29bd999f36b766df9cda90906ad53d`.
- `noise-vectors.json`: the Noise Protocol Framework test vectors published with the Cacophony project (`vectors/cacophony.txt`), reduced to the `Noise_*_25519_ChaChaPoly_SHA256` entries for the 15 fundamental patterns (the full file is over 1 MB). Fields are unchanged.

The RFC 7748, 8439 and 5869 vectors are written into the tests themselves.
