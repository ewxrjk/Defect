; Registers:
;   eax   return
;   ebx   preserve
;   ecx   smash
;   edx   smash
;   esi   preserve
;   edi   preserve
;   esp
;   ebp   preserve

        .model flat
        .code

; int cyclic_moore(byte *from, byte *to,
;                  int width, int states);
; NB we don't handle the first or last row.
cyclic_moore:
        push ebx
        push esi
        push edi
        push ebp
        mov esi,[esp + 16 + 4]    ; esi = from
        mov edi,[esp + 16 + 8]    ; edi = to
        mov ecx,[esp + 16 + 12]
        mov bl,[esp + 16 + 16]    ; bl = states
        mov edx,ecx               ; edx = width
        mov ebp,edx
        neg ebp                   ; ebp = -width
        sub ecx,2                 ; ecx = cells left
        mov eax,edx               ; eax = change count
; first cell
        mov bh,[esi]
        inc bh
        cmp bh,bl
        jb first_nomod
        xor bh,bh
first_nomod:
        cmp bh,[esi+edx-1]        ; left (wraps)
        je first_store
        cmp bh,[esi+1]            ; right
        je first_store
        cmp bh,[esi+2*edx-1]      ; down left (wraps)
        je first_store
        cmp bh,[esi+edx]          ; down
        je first_store
        cmp bh,[esi+edx+1]        ; down right
        je first_store
        cmp bh,[esi-1]            ; up left (wraps)
        je first_store
        cmp bh,[esi+ebp]          ; up
        je first_store
        cmp bh,[esi+ebp+1]        ; up right
        je first_store
        mov bh,[esi]              ; reload original state
        dec eax
first_store:
        mov [edi],bh              ; store new state
        inc esi
        inc edi
; all but first and last cells
        align 16
mainloop:
        mov bh,[esi]
        inc bh
        cmp bh,bl
        jb main_nomod
        xor bh,bh
main_nomod:
        cmp bh,[esi-1]            ; left
        je main_store
        cmp bh,[esi+1]            ; right
        je main_store
        cmp bh,[esi+edx-1]        ; down left
        je main_store
        cmp bh,[esi+edx]          ; down
        je main_store
        cmp bh,[esi+edx+1]        ; down right
        je main_store
        cmp bh,[esi+ebp-1]        ; up left
        je main_store
        cmp bh,[esi+ebp]          ; up
        je main_store
        cmp bh,[esi+ebp+1]        ; up right
        je main_store
        mov bh,[esi]              ; reload original state
        dec eax
main_store:
        mov [edi],bh              ; store new state
        inc esi
        inc edi
        dec ecx
        jnz mainloop
; last cell
        mov bh,[esi]
        inc bh
        cmp bh,bl
        jb last_nomod
        xor bh,bh
last_nomod:
        cmp bh,[esi-1]            ; left
        je last_store
        cmp bh,[esi+ebp+1]        ; right (wraps)
        je last_store
        cmp bh,[esi+edx-1]        ; down left
        je last_store
        cmp bh,[esi+edx]          ; down
        je last_store
        cmp bh,[esi+1]            ; down right (wraps)
        je last_store
        cmp bh,[esi+ebp-1]        ; up left
        je last_store
        cmp bh,[esi+ebp]          ; up
        je last_store
        cmp bh,[esi+2*ebp+1]      ; up right (wraps)
        je last_store
        mov bh,[esi]              ; reload original state
        dec eax
last_store:
        mov [edi],bh              ; store new state
; done
        pop ebp
        pop edi
        pop esi
        pop ebx
        ret

        PUBLIC cyclic_moore

        END