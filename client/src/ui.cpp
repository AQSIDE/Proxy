#include "ui.h"

void Panel::update(AppContext* app) {
    UIElement::update(app);
}

void Panel::draw(AppContext* app) {
    if (!isVisible) return;

    DrawRectangleRec(getRect(), isHovered ? hoverColor : color);
}

void InputField::update(AppContext* app) {
    if (!isActive) return;
    UIElement::update(app);

    if (isHovered) {
        app->frame.cursor.set(CursorType::IBEAM);
    }

    if (app->frame.leftClickPressed) {
        selected = isHovered;
    }

    if (selected) {
        int c = GetCharPressed();

        if (c > 0 && text.size() <= maxChars) {
            text += (char)c;
        }

        if ((IsKeyPressedRepeat(KEY_BACKSPACE) || IsKeyPressed(KEY_BACKSPACE)) && !text.empty()) {
            text.pop_back();
        }
    }
}

void InputField::draw(AppContext* app) {
    if (!isVisible) return;

    Color targetColor = selected ? selectedColor : (isHovered ? hoverColor : color);
    auto rect = getRect();

    DrawRectangleRec(rect, targetColor);

    float paddingX = 15.0f;
    float textY = rect.y + (rect.height / 2.0f) - (STYLE::fontSize / 2.0f);

    if (text.empty() && !placeholder.empty()) {
        DrawText(placeholder.c_str(), rect.x + paddingX, textY, STYLE::fontSize, COLORS::TEXT_SECONDARY);
    }
    else {
        DrawText(text.c_str(), rect.x + paddingX, textY, STYLE::fontSize, COLORS::TEXT_MAIN);
    }

    if (selected) {
        float time = GetTime();
        if ((int)(time * 2.0f) % 2 == 0) {
            int textWidth = MeasureText(text.c_str(), STYLE::fontSize);
            float cursorX = rect.x + paddingX + textWidth + 2;

            DrawRectangle(cursorX, rect.y + 10, 2, rect.height - 20, COLORS::TEXT_MAIN);
        }
    }
}

void Label::update(AppContext* app) {
    if (!isActive) return;
    UIElement::update(app);

    if (isHovered) {
        app->frame.cursor.set(CursorType::IBEAM);
    }
}

void Label::draw(AppContext* app) {
    if (!isVisible) return;

    Font font = GetFontDefault();
    float spacing = 1.0f;

    Vector2 textSize = MeasureTextEx(font, text.c_str(), fontSize, spacing);

    Vector2 drawPos = {
        position.x - textSize.x * pivot.x,
        position.y - textSize.y * pivot.y,
    };

    DrawTextEx(font, text.c_str(), drawPos, fontSize, spacing, color);
}

void Button::update(AppContext* app) {
    if (!isActive) return;
    UIElement::update(app);

    if (isHovered) {
        app->frame.cursor.set(CursorType::POINTER);
    }
}

void Button::draw(AppContext* app) {
    if (!isVisible) return;
    Color target = color;
    if (isHovered) {
        target = hoverColor;
    }

    auto rect = getRect();
    DrawRectangleRec(rect, target);

    if (!label.text.empty()) {
        label.position = { rect.x + rect.width / 2, rect.y + rect.height / 2 };
        label.draw(app);
    }
}