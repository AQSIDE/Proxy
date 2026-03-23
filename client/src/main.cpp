#include "common.h"
#include "layout.h"
#include "proxy.h"
#include "ui.h"

int main() {
    SetConfigFlags(FLAG_WINDOW_RESIZABLE);
    InitWindow(400, 600, "Proxy");
    SetTargetFPS(60);

    UIManager ui;
    AppContext app;

    auto* nameLabel = new Label("PROXY");
    nameLabel->fontSize = 32;

    auto* host = new InputField(COLORS::INPUT_BG);
    host->text = "127.0.0.1";
    auto* port = new InputField(COLORS::INPUT_BG);
    port->text = "8888";
    auto* hostLabel = new Label("HOST:");
    hostLabel->pivot = { 0, 0.5f };
    auto* portLabel = new Label("PORT:");
    auto* warningLabel = new Label("NOT CONNECTED", 16);
    warningLabel->isVisible = false;
    warningLabel->color = COLORS::TEXT_YELLOW;
    portLabel->pivot = { 0, 0.5f };

    auto connectBtn = new Button(COLORS::BTN_GREEN);
    connectBtn->label.text = "CONNECT";

    ui.add("name_label", nameLabel);
    ui.add("host_field", host);
    ui.add("port_field", port);
    ui.add("host_label", hostLabel);
    ui.add("port_label", portLabel);
    ui.add("connect_btn", connectBtn);
    ui.add("warning_label", warningLabel);

    while (!WindowShouldClose()) {
        app.update();
        ui.update(&app);
        updateMainView(&app, &ui);

        BeginDrawing();
        ClearBackground(COLORS::MAIN_BG);

        ui.draw(&app);
        drawMainView(&app, &ui);

        app.frame.cursor.apply();
        EndDrawing();
    }

    setProxy("", false);
    CloseWindow();
    return 0;
}