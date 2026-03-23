#pragma once

#include "common.h"

enum class CursorType {
    DEFAULT = 0,
    POINTER = 4,
    IBEAM = 2,
    RESIZE_H = 5,
    RESIZE_V = 6
};

struct CursorContext {
    CursorType current = CursorType::DEFAULT;

    void reset() { current = CursorType::DEFAULT; }

    void set(CursorType type) {
        current = type;
    }

    void apply() {
        SetMouseCursor((int)current);
    }
};

struct FrameContext {
    CursorContext cursor;
    Vector2 mousePos;
    Vector2 windowSize;
    bool leftClickPressed;
    bool leftClickReleased;
    float scroll;
    float dt;
};

class AppContext {
public:
    FrameContext frame;

    AppContext() : frame() {}

    inline void update() {
        frame.cursor.reset();
        frame.scroll = GetMouseWheelMove();
        frame.mousePos = GetMousePosition();
        frame.dt = GetFrameTime();
        frame.leftClickPressed = IsMouseButtonPressed(MOUSE_BUTTON_LEFT);
        frame.leftClickReleased = IsMouseButtonReleased(MOUSE_BUTTON_LEFT);
        frame.windowSize = { (float)GetScreenWidth(), (float)GetScreenHeight() };
    }
};