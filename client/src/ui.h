#pragma once

#include "common.h"

class UIElement {
public:
    Vector2 position;
    Vector2 size;

    Color color;

    bool isHovered;
    bool leftClickReleased;
    bool leftClickPressed;

    bool isActive;
    bool isVisible;

    UIElement(Vector2 pos, Vector2 size, Color color) : position(pos), color(color), size(size), isActive(true), isVisible(true) {}
    UIElement(Vector2 size, Color color) : position({ 0 }), size(size), color(color), isActive(true), isVisible(true) {}

    virtual ~UIElement() {}

    virtual void update(AppContext* app) {
        Rectangle rect = getRect();

        isHovered = CheckCollisionPointRec(app->frame.mousePos, rect);
        leftClickReleased = isHovered && app->frame.leftClickReleased;
        leftClickPressed = isHovered && app->frame.leftClickPressed;
    }

    virtual void draw(AppContext* app) = 0;

    inline Rectangle getRect() const {
        return { position.x, position.y, size.x, size.y };
    }

    inline void setRect(Rectangle rect) {
        position.x = rect.x;
        position.y = rect.y;
        size.x = rect.width;
        size.y = rect.height;
    }
};

class UIManager {
public:
    map<string, UIElement*> elements;

    void add(string name, UIElement* el) {
        elements[name] = el;
    }

    void update(AppContext* app) {
        for (auto const& [name, el] : elements) {
            if (el->isActive) el->update(app);
        }
    }

    void draw(AppContext* app) {
        for (auto const& [name, el] : elements) {
            if (el->isVisible) el->draw(app);
        }
    }

    UIElement* getElement(string name) { return elements[name]; }

    template<typename T>
    T* get(const std::string& name) {
        UIElement* element = getElement(name);

        if (!element) return nullptr;
        return dynamic_cast<T*>(element);
    }

    ~UIManager() {
        for (auto const& [name, el] : elements) delete el;
        elements.clear();
    }
};

class Panel : public UIElement {
public:
    Color hoverColor;

    Panel(Vector2 size, Color color) : UIElement(size, color), hoverColor(ColorBrightness(color, -0.2f)) {
    }

    Panel(Color color) : UIElement({ 0 }, color), hoverColor(ColorBrightness(color, -0.2f)) {
    }

    void update(AppContext* app) override;
    void draw(AppContext* app) override;
};

class InputField : public UIElement {
public:
    Color hoverColor;
    Color selectedColor;
    bool selected;

    string text;
    string placeholder;
    int maxChars;

    InputField(Color color) : UIElement({ 0 }, color), maxChars(20), hoverColor(ColorBrightness(color, -0.2f)), selectedColor(ColorBrightness(color, 0.05f)) {
    }

    void update(AppContext* app) override;
    void draw(AppContext* app) override;
};

class Label : public UIElement {
public:
    string text;
    int fontSize;
    Vector2 pivot;

    Label(string text, int fontSize = 20, Vector2 pivot = { 0.5f, 0.5f }, Color color = WHITE) : UIElement({ 0 }, color), pivot(pivot), fontSize(fontSize), text(text) {
    }

    void update(AppContext* app) override;
    void draw(AppContext* app) override;
};

class Button : public UIElement {
public:
    Label label;
    Color hoverColor;

    Button(Color color) : UIElement({ 0 }, color), hoverColor(ColorBrightness(color, -0.2f)), label("Button") {
    }

    void update(AppContext* app) override;
    void draw(AppContext* app) override;
};