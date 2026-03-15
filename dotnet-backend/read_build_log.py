import sys

try:
    with open('build_err.txt', 'rb') as f:
        content = f.read()
    
    # Try decoding as utf-16-le
    try:
        text = content.decode('utf-16-le')
    except:
        text = content.decode('utf-8', errors='ignore')
        
    print(text)
except Exception as e:
    print(f"Error reading file: {e}")
