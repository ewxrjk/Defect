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

; See http://msdn.microsoft.com/en-us/library/ms235286.aspx for ABI
;
;   reg   ABI                        usage here
;   rax   return                     transient misce
;   rbx   preserve                   transient old cell value
;   rcx   first integer arg          change counter
;   rdx   second integer arg         transient loop counter
;   rsi   preserve                   'from' pointer
;   rdi   preserve                   'to' pointer
;   rsp
;   rbp   preserve
;   r8    third integer arg          width
;   r9    fourth integer arg         states
;   r10   smash                      cell counter
;   r11   smash                      -width
;   r12   preserve
;   r13   preserve
;   r14   preserve
;   r15   preserve
;   xmm0  smash                      {cell + 1 (mod states)} x 16
;   xmm1  smash                      {01} x 16
;   xmm2  smash                      {states} x 16
;   xmm3  smash                      left; overflow mask
;   xmm4  smash                      right
;   xmm5  smash                      down
;   xmm6  preserve                   up
;   xmm7  preserve                   {FF} x 16
;   xmm8  preserve                   stashed cell values
;   xmm9  preserve                   overflow mask
;   xmm10+ preserve
;
; Of the args we only preserve r9.
	
        .code

; int cyclic_vn_64(byte *from [rcx], byte *to [rdx],
;                  int width [r8], int states [r9]);
; Returns change count
; NB we don't handle the first or last row.
cyclic_vn_64:
        push rdi
        push rsi
        push rbx
        sub rsp,16*4
        movdqa [rsp],xmm6
        movdqa [rsp+16],xmm7
        movdqa [rsp+32],xmm8
        movdqa [rsp+48],xmm9
        mov rsi,rcx            ; rsi = source
        mov rdi,rdx            ; rdi = destination
        mov r10,r8
        mov r11,r8
        sub r10,2              ; r10 = cells left
        neg r11                ; r11 = -width
        xor rcx,rcx            ; rcx = change count
; first cell
        mov al,[rsi]
        mov bl,al
        inc al
        cmp al,r9b
        jb first_nomod
        xor rax,rax
first_nomod:
        cmp al,[rsi+r8-1]      ; left (wrapped)
        je first_store
        cmp al,[rsi+1]         ; right
        je first_store
        cmp al,[rsi+r8]        ; down
        je first_store
        cmp al,[rsi+r11]       ; up
        je first_store
        mov al,bl              ; retrieve original
        dec rcx
first_store:
        mov [rdi],al
        inc rcx
        inc rsi
        inc rdi
; process 16-byte "paragraphs"
; xmm1 = 01 in all byte positions
        pxor xmm0,xmm0
        mov rax,1
        movd xmm1,rax
        pshufb xmm1,xmm0
; xmm2 = <states> in all byte positions
        movd xmm2,r9
        pshufb xmm2,xmm0
; xmm7 = ff in all byte positions
        mov rax,0FFh
        movd xmm7,rax
        pshufb xmm7,xmm0
; figure out how much work to do with SSE2
; (1) the work happens in 16-byte chunks
; (2) wrapping must be avoided in the last few bytes
; The former is easy. For the latter we require that rsi+1+15 is before the
; end of the cell row.  That means we cannot handle the last 16 cells here.
; We already special-case the last one so that just means we need to drop
; 15 from the count.
        mov rdx,r10
        sub rdx,15                ; can't do the last 15 bytes
        jbe trailer
        shr rdx,4                 ; scale down to dqwords
        je trailer
        mov rax,rdx
        shl rax,4                 ; scale back up to bytes
        sub r10,rax               ; length of trailer
        align 16
mainloop:
        movdqu xmm0,[rsi]         ; get {cell} x 16
; get 4 x {neighbour} x 16
        movdqu xmm3,[rsi-1]       ; left
        movdqu xmm4,[rsi+1]       ; right
        movdqu xmm5,[rsi+r8]      ; down
        movdqu xmm6,[rsi+r11]     ; up
; find next-state values
        movdqu xmm8,xmm0          ; stash originals
        paddb xmm0,xmm1           ; find {cell + 1} x 16
        movdqa xmm9,xmm0
        pcmpeqb xmm0,xmm2         ; ff for equal, 00 otherwise
        pandn xmm0,xmm9           ; reset overflowed cell values to 0
; now we have xmm0 = {cell + 1 (mod states)} x 16
; detect matching values, leaving xmm4-7 with ff for each position with
; a match and 00 elsewhere.
        pcmpeqb xmm3,xmm0
        pcmpeqb xmm4,xmm0
        pcmpeqb xmm5,xmm0
        pcmpeqb xmm6,xmm0
; combine the results, leaving xmm3 with ff for each position with a match
; in any neighbour and 00 elsewhere.
        por xmm3,xmm4
        por xmm5,xmm6
        por xmm3,xmm5
; now:
;  xmm0 = {cell + 1 (mod states)} x 16
;  xmm3 = {ff for updated, 00 for unchanged} x 16
;  xmm8 = {cell} x 16
; store {cell+1 (mod states)} x 16 but only where matches were found
        pmovmskb rax,xmm3
        pand xmm0,xmm3             ; mask out unchanged
        pxor xmm3,xmm7
        pand xmm8,xmm3             ; mask out changed
        por xmm0,xmm8              ; combine
        movdqu [rdi],xmm0
; count how many changes we made
        popcnt rax,rax
        add rcx,rax
        add rsi,16
        add rdi,16
        dec rdx
        jnz mainloop
; do the last r10 bytes individually
trailer:
        cmp r10,0
        je last
        add rcx,r10
byte_loop:
        mov al,[rsi]           ; get cell value
        mov bl,al              ; stash it for later
        inc al
        cmp al,r9b
        jb byte_nomod
        xor rax,rax            ; wrapped around
byte_nomod:
        cmp al,[rsi-1]         ; left
        je byte_store
        cmp al,[rsi+1]         ; right
        je byte_store
        cmp al,[rsi+r8]        ; down
        je byte_store
        cmp al,[rsi+r11]       ; up
        je byte_store
        mov al,bl              ; retrieve original
        dec rcx
byte_store:
        mov [rdi],al
        inc rsi
        inc rdi
        dec r10
        jnz byte_loop
; last cell is special-cased
last:
        mov al,[rsi]
        mov bl,al
        inc al
        cmp al,r9b
        jb last_nomod
        xor rax,rax
last_nomod:
        cmp al,[rsi-1]         ; left
        je last_store
        cmp al,[rsi+r11+1]     ; right (wrapped)
        je last_store
        cmp al,[rsi+r8]        ; down
        je last_store
        cmp al,[rsi+r11]       ; up
        je last_store
        mov al,bl              ; retrieve original
        dec rcx
last_store:
        mov [rdi],al
        inc rcx
        mov rax,rcx
        movdqa xmm6,[rsp]
        movdqa xmm7,[rsp+16]
        movdqa xmm8,[rsp+32]
        movdqa xmm9,[rsp+48]
        add rsp,16*4
        pop rbx
        pop rsi
        pop rdi
        ret

; int cyclic_vn_64_all(byte *from [rcx], byte *to [rdx],
;                      int width [r8], int states [r9],
;                      int height [stack])
; Returns change count
; Buffers must contain two more rows than implied by height!
cyclic_vn_64_all:
        push rsi
        push rdi
        push rbx
        push r10
        push r11
        push r12
        push r13
        cld
        mov rbx,[rsp+96]
        mov r10,rcx            ; from
        mov r11,rdx            ; to
        mov r12,r8             ; width
        mov rax,r8
        mul rbx                ; rax = width * height
; copy the first row to just after the end
        lea rsi,[r10+r12]      ; rsi = initial row
        lea rdi,[rsi+rax]      ; rdi = post-final row
        mov rcx,r12
        rep movsb              ; copy
; copy the final row to just before the beginning
        mov rdi,r10            ; rdi = pre-initial row
        lea rsi,[rdi+rax]      ; rsi = final row
        mov rcx,r12
        rep movsb
; set up initial values
        lea rsi,[r10+r12]
        lea rdi,[r11+r12]
        xor r13,r13
        align 16
all_loop:
        mov rcx,rsi
        mov rdx,rdi
        mov r8,r12
        ; r9 is right already
        call cyclic_vn_64
        add r13,rax
        add rsi,r12
        add rdi,r12
        dec rbx
        jnz all_loop
        mov rax,r13
        pop r13
        pop r12
        pop r11
        pop r10
        pop rbx
        pop rdi
        pop rsi
        ret

        PUBLIC cyclic_vn_64_all

        END
