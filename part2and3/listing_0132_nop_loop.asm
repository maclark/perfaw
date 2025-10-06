; this is a comment i believe (semi colons)
; we needed some weird global shit i don't remember

global MOVAllBytesASM
global NOPAllBytesASM
global CMPAllBytesASM
global DECAllBytesASM

section .text

; by the ABI (application binary interface), we know first integer arg is in rcx, second is in rdx

MOVAllBytesASM:
    xor rax, rax ; convention for zeroing out a register
.loop:
    mov [rdx + rax], al
    inc rax
    cmp rax, rcx
    jb .loop
    ret

NOPAllBytesASM:
    xor rax, rax
.loop:
    db 0x0f, 0x1f, 0x00 ; casey said he looked this up in the manual to make sure it was a 3 byte nop
    inc rax
    cmp rax, rcx
    jb .loop
    ret

CMPAllBytesASM:
    xor rax, rax
.loop:
    inc rax
    cmp rax, rcx
    jb .loop
    ret

DECAllBytesASM:
    xor rax, rax
.loop:
    dec rcx
    cmp rax, rcx
    jnz .loop
    ret

