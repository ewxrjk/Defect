; This program is © 2013 Richard Kettlewell.
;
; This program is free software: you can redistribute it and/or modify
; it under the terms of the GNU General Public License as published by
; the Free Software Foundation, either version 3 of the License, or
; (at your option) any later version.
; 
; This program is distributed in the hope that it will be useful,
; but WITHOUT ANY WARRANTY; without even the implied warranty of
; MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
; GNU General Public License for more details.
; 
; You should have received a copy of the GNU General Public License
; along with this program.  If not, see <http://www.gnu.org/licenses/>.
;

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

; int cyclic_vn_32(byte *from, byte *to,
;                  int width, int states);
; NB we don't handle the first or last row.
cyclic_vn_32:
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
        cmp bh,[esi+edx-1]        ; left (wrapped)
        je first_store
        cmp bh,[esi+1]            ; right
        je first_store
        cmp bh,[esi+edx]          ; down
        je first_store
        cmp bh,[esi+ebp]          ; up
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
        cmp bh,[esi+edx]          ; down
        je main_store
        cmp bh,[esi+ebp]          ; up
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
        cmp bh,[esi+ebp+1]        ; right (wrapped)
        je last_store
        cmp bh,[esi+edx]          ; down
        je last_store
        cmp bh,[esi+ebp]          ; up
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

; int cyclic_vn_32_all(byte *from [esp+24h], byte *to [esp+28h],
;                      int width [esp+2ch], int states [esp+30h],
;                      int height [esp+34h])
; Returns change count
; Buffers must contain two more rows than implied by height!
cyclic_vn_32_all:
        cld
        push ebx
        push ebp
        push esi
        push edi
        sub esp,10h
        mov ecx,[esp+2ch]      ; ecx = width
        mov eax,[esp+34h]
        mul ecx                ; eax = width * height
        mov ebp,ecx            ; ebp = width
; copy the first row to just after the end
        mov esi,[esp+24h]
        add esi,ecx            ; esi = initial row
        lea edi,[esi+eax]      ; edi = post-final row
        rep movsb
; copy the final row to just before the beginning
        mov edi,[esp+24h]      ; edi = pre-initial row
        lea esi,[edi+eax]      ; esi = final row
        mov ecx,ebp            ; ecx = width
        rep movsb
; set up fixed arguments
        mov edx,[esp+30h]      ; edx = states
        mov [esp+08h],ebp      ; width
        mov [esp+0ch],edx   
; set up initial values
        mov esi,[esp+24h]      ; esi = from
        mov edi,[esp+28h]      ; edi = to
        mov ebx,[esp+34h]      ; ebx = height
        add esi,ebp
        add edi,ebp
        xor ebp,ebp
        align 16
loop_all:
        mov [esp+00h],esi
        mov [esp+04h],edi
        call cyclic_vn_32
        mov ecx,[esp+2ch]
        add ebp,eax
        add esi,ecx
        add edi,ecx
        dec ebx
        jnz loop_all
        mov eax,ebp
        add esp,10h
        pop edi
        pop esi
        pop ebp
        pop ebx
        ret

        PUBLIC cyclic_vn_32_all

        END