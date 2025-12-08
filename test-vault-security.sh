#!/bin/bash

# CoreVault Security Module - Quick Test Script
# This script demonstrates the vault functionality

API_BASE="http://localhost:5000"
API_KEY="test-key-for-ci"
PASSPHRASE="demo-passphrase-123"

echo "=== CoreVault Security Module Demo ==="
echo

# Test 1: Store a secret
echo "1. Storing secret..."
curl -s -X POST "$API_BASE/api/security/vault" \
  -H "Content-Type: application/json" \
  -H "X-API-Key: $API_KEY" \
  -H "X-Vault-Passphrase: $PASSPHRASE" \
  -d '{
    "key": "demo-secret",
    "value": "this-is-a-very-secret-value"
  }' | jq .
echo

# Test 2: Retrieve the secret
echo "2. Retrieving secret..."
curl -s -X GET "$API_BASE/api/security/vault/demo-secret" \
  -H "X-API-Key: $API_KEY" \
  -H "X-Vault-Passphrase: $PASSPHRASE" | jq .
echo

# Test 3: Check if secret exists
echo "3. Checking if secret exists..."
curl -s -X GET "$API_BASE/api/security/vault/demo-secret/exists" \
  -H "X-API-Key: $API_KEY" | jq .
echo

# Test 4: Try with wrong passphrase (should fail)
echo "4. Testing with wrong passphrase..."
curl -s -X GET "$API_BASE/api/security/vault/demo-secret" \
  -H "X-API-Key: $API_KEY" \
  -H "X-Vault-Passphrase: wrong-passphrase" | jq .
echo

# Test 5: Try to get non-existent secret
echo "5. Testing non-existent secret..."
curl -s -X GET "$API_BASE/api/security/vault/nonexistent" \
  -H "X-API-Key: $API_KEY" \
  -H "X-Vault-Passphrase: $PASSPHRASE" | jq .
echo

# Test 6: Delete the secret
echo "6. Deleting secret..."
curl -s -X DELETE "$API_BASE/api/security/vault/demo-secret" \
  -H "X-API-Key: $API_KEY" | jq .
echo

# Test 7: Verify deletion
echo "7. Verifying secret was deleted..."
curl -s -X GET "$API_BASE/api/security/vault/demo-secret/exists" \
  -H "X-API-Key: $API_KEY" | jq .
echo

echo "=== Demo Complete ==="
