#include "layout.h"
#include "proxy.h"

static Vector2 appSize;

static bool isConnected = false;

void drawMainView(AppContext* app, UIManager* ui) {
    appSize = app->frame.windowSize;

    auto nameApp = ui->get<Label>("name_label");
    auto host = ui->get<InputField>("host_field");
    auto port = ui->get<InputField>("port_field");
    auto portLabel = ui->get<Label>("port_label");
    auto hostLabel = ui->get<Label>("host_label");
    auto connectBtn = ui->get<Button>("connect_btn");
    auto warningLabel = ui->get<Label>("warning_label");

    nameApp->position = { appSize.x / 2, 30 };
    float startY = nameApp->position.y + 20;

    Vector2 fieldSize = { appSize.x / 1.3f, 50 };
    Rectangle hostRect = {
        appSize.x / 2 - fieldSize.x / 2,
        startY + 80,
        fieldSize.x,
        fieldSize.y
    };

    hostLabel->position = { hostRect.x, hostRect.y - 15 };

    Rectangle portRect = {
        appSize.x / 2 - fieldSize.x / 2,
        startY + 200,
        fieldSize.x,
        fieldSize.y
    };

    portLabel->position = { portRect.x, portRect.y - 15 };

    host->setRect(hostRect);
    port->setRect(portRect);

    float btnWidth = 200;
    float btnHeight = 50;
    Rectangle connectBtnRect = {
        appSize.x / 2 - btnWidth / 2,
        appSize.y - btnHeight - 50,
        btnWidth,
        btnHeight
    };

    connectBtn->setRect(connectBtnRect);
    warningLabel->position = {
    connectBtnRect.x + connectBtnRect.width / 2,
    connectBtnRect.y + connectBtnRect.height + 25
    };
}

void updateMainView(AppContext* app, UIManager* ui) {
    auto connectBtn = ui->get<Button>("connect_btn");

    if (connectBtn->leftClickReleased) {
        if (isConnected) {
            setProxy("", false);

            isConnected = false;
            connectBtn->color = COLORS::BTN_GREEN;
            connectBtn->hoverColor = ColorBrightness(COLORS::BTN_GREEN, -0.2f);
            connectBtn->label.text = "CONNECT";
        }
        else {
            auto warningLabel = ui->get<Label>("warning_label");
            auto host = ui->get<InputField>("host_field");
            auto port = ui->get<InputField>("port_field");

            bool connected = setProxy(createAddress(host->text, port->text), true);
            if (connected) {
                isConnected = true;
                warningLabel->isVisible = false;
                connectBtn->color = COLORS::BTN_RED;
                connectBtn->hoverColor = ColorBrightness(COLORS::BTN_RED, -0.2f);
                connectBtn->label.text = "DISCONNECT";
            }
            else {
                warningLabel->isVisible = true;
                warningLabel->text = "CONNECTING ERROR";
            }
        }
    }
}
