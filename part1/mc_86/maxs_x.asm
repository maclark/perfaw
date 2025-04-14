bits 16

mov bp, 64*4 ; 64x64, skipping one row bc that's where our code will be stores in the byte array
mov ax, 64 ; the width/height of our square 
mov dx, 0 ; y-coord 
y_loop_start:
	
	mov cx, 0 ; x-coord (ip:0x9->0xC)
	x_loop_start:
        cmp cx, dx 
        je fill_pix_color ; if x == y, this is the backslash
        ; check for backslash
        ; when it reaches 64 - x - 1 == y, then it will also fill
        mov bx, ax
        sub bx, cx
        sub bx, 1
        cmp dx, bx ; maybe we can just do cmp dx, 64 - cx - 1 
        je fill_pix_color
        jne fill_pix_black
			
        keep_looping:
		; Advance pixel location
		add bp, 4
			
		; Advance X coordinate and loop
		add cx, 1
		cmp cx, ax ; ax holds the size of our square
		jnz x_loop_start
	
	; Advance Y coordinate and loop
	add dx, 1
	cmp dx, ax
	jnz y_loop_start
    je finish_line
fill_pix_color:
    mov word [bp + 0], 255 ; Red
    mov byte [bp + 3], 255 ; Alpha
    ;ret
    je keep_looping
fill_pix_black:
    mov word [bp + 2], 255 ; Blue
    mov byte [bp + 3], 255 ; Alpha
    ;ret
    jne keep_looping
finish_line:
