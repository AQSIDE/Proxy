#pragma once

#include "raylib.h"

namespace COLORS {
    const Color INPUT_BG = { 23, 33, 43, 255 };    // #17212b (Тот же, что TG_SIDEBAR_BG)
    const Color SIDEBAR_BG = { 23,  33,  43,  255 }; // #17212b
    const Color MAIN_BG = { 14,  22,  33,  255 }; // #0e1621
    const Color SELECTED_CHAT = { 43,  82,  120, 255 }; // #2b5278
    const Color HOVER_CHAT = { 43,  56,  70,  255 }; // #2b3846

    const Color MY_MESSAGE = { 43,  82,  120, 255 }; // #2b5278
    const Color OTHER_MESSAGE = { 24,  37,  51,  255 }; // #182533

    const Color TEXT_MAIN = { 255, 255, 255, 255 };
    const Color TEXT_SECONDARY = { 127, 145, 164, 255 }; // #7f91a4
    const Color TEXT_YELLOW = { 255, 193, 7, 255 };
    const Color BTN_GREEN = { 78,  175, 79,  255 }; // #4eaf4f
    const Color BTN_RED = { 255, 0,   0,  255 }; // #FF0000
}

namespace STYLE {
    const int fontSize = 20;
}