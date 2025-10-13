; let's see!

global NOP3x1AllBytes
global NOP1x3AllBytes
global NOP1x9AllBytes

section .text

NOP3x1AllBytes:
    xor rax, rax
.loop:
    db 0x0f, 0x1f, 0x00 ; from the manual
    inc rax
    cmp rax, rcx
    jb .loop
    ret

NOP1x3AllBytes:
    xor rax, rax
.loop:
    nop
    nop
    nop
    inc rax
    cmp rax, rcx
    jb .loop
    ret

NOP1x9AllBytes:
    xor rax, rax
.loop:
    nop
    nop
    nop
    nop
    nop
    nop
    nop
    nop
    nop
    inc rax
    cmp rax, rcx
    jb .loop
    ret
