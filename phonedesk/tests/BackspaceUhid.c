// SPDX-License-Identifier: Apache-2.0
#include <assert.h>
#include <stdio.h>
#include <string.h>
#include "uhid/keyboard_uhid.h"
#include <SDL3/SDL_keyboard.h>

static unsigned count;
static unsigned char reports[16][8];
bool sc_thread_is_main(void) {return true;}

bool sc_controller_push_msg(struct sc_controller *controller,const struct sc_control_msg *msg) {
    (void)controller;if(msg->type!=SC_CONTROL_MSG_TYPE_UHID_INPUT)return true;assert(count<16);assert(msg->uhid_input.size==8);
    memcpy(reports[count++],msg->uhid_input.data,8);return true;
}
int main(void) {
    struct sc_keyboard_uhid kb;
    assert(sc_keyboard_uhid_init(&kb,NULL));
    struct sc_key_event event={.action=SC_ACTION_DOWN,.scancode=SC_SCANCODE_BACKSPACE};
    kb.key_processor.ops->process_key(&kb.key_processor,&event,0);
    assert(count==2);assert(reports[0][2]==42);assert(reports[1][2]==0);
    event.repeat=true;
    kb.key_processor.ops->process_key(&kb.key_processor,&event,0);
    assert(count==4);assert(reports[2][2]==42);assert(reports[3][2]==0);
    event.action=SC_ACTION_UP;event.repeat=false;
    kb.key_processor.ops->process_key(&kb.key_processor,&event,0);
    assert(count==4);
    event.scancode=SC_SCANCODE_A;event.action=SC_ACTION_DOWN;
    kb.key_processor.ops->process_key(&kb.key_processor,&event,0);
    assert(count==5&&reports[4][2]==4);
    event.repeat=true;
    kb.key_processor.ops->process_key(&kb.key_processor,&event,0);
    assert(count==5);
    event.repeat=false;event.scancode=SC_SCANCODE_BACKSPACE;event.mods_state=SC_MOD_LSHIFT;
    kb.key_processor.ops->process_key(&kb.key_processor,&event,0);
    assert(count==7&&reports[5][0]==2&&reports[6][0]==2&&reports[6][2]==4);
    puts("PASS: Backspace tap/repeat/release, other keys and modifiers (7 checks)");
}
