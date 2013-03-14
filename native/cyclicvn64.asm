; See http://msdn.microsoft.com/en-us/library/ms235286.aspx for ABI
;
; Registers:
;   rax   return
;   rbx   preserve
;   rcx   first integer arg
;   rdx   second integer arg
;   rsi   preserve
;   rdi   preserve
;   rsp
;   rbp   preserve
;   r8    third integer arg
;   r9    fourth integer arg
;   r10   smash
;   r11   smash
;   r12   preserve
;   r13   preserve
;   r14   preserve
;   r15   preserve
;   xmm0-5   smash
;   xmm6-16  preserve

	
        .code
; int cyclic_vn_64(byte *from [rcx], byte *to [rdx],
;                  int width [r8], int states [r9]);
; Returns change count
; NB we don't handle the first or last row.
cyclic_vn_64:
        push rsi
        push rbx
        mov r10,r8
        mov r11,r8
        sub r10,2              ; r10 = cells left
        neg r11                ; r11 = -width
        mov rsi,r8             ; rsi = change count
; first cell
        mov al,[rcx]
        mov bl,al
        inc al
        cmp al,r9b
        jb first_nomod
        xor rax,rax
first_nomod:
        cmp al,[rcx+r8-1]      ; left (wrapped)
        je first_store
        cmp al,[rcx+1]         ; right
        je first_store
        cmp al,[rcx+r8]        ; down
        je first_store
        cmp al,[rcx+r11]       ; up
        je first_store
        mov al,bl              ; retrieve original
        dec rsi
first_store:
        mov [rdx],al
        inc rcx
        inc rdx
; all but first and last cells
        align 16
mainloop:
        mov al,[rcx]           ; get cell value
        mov bl,al              ; stash it for later
        inc al
        cmp al,r9b
        jb main_nomod
        xor rax,rax            ; wrapped around
main_nomod:
        cmp al,[rcx-1]         ; left
        je main_store
        cmp al,[rcx+1]         ; right
        je main_store
        cmp al,[rcx+r8]        ; down
        je main_store
        cmp al,[rcx+r11]       ; up
        je main_store
        mov al,bl              ; retrieve original
        dec rsi
main_store:
        mov [rdx],al
        inc rcx
        inc rdx
        dec r10
        jnz mainloop
; last cell
        mov al,[rcx]
        mov bl,al
        inc al
        cmp al,r9b
        jb last_nomod
        xor rax,rax
last_nomod:
        cmp al,[rcx-1]         ; left
        je last_store
        cmp al,[rcx+r11+1]     ; right (wrapped)
        je last_store
        cmp al,[rcx+r8]        ; down
        je last_store
        cmp al,[rcx+r11]       ; up
        je last_store
        mov al,bl              ; retrieve original
        dec rsi
last_store:
        mov [rdx],al
        mov rax,rsi
        pop rbx
        pop rsi
        ret

        PUBLIC cyclic_vn_64

        END
