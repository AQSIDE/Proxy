#pragma once

#include <string>
#include <cstdint>

const std::string PACKET_MAGIC = "PRXY";
const uint8_t PACKET_CONN = 1;
const uint8_t PACKET_CLOSE = 2;
const uint8_t PACKET_TRAFFIC = 3;

struct ConnectPacket {
    uint8_t id;
    char magic[4];;

    ConnectPacket() : id(PACKET_CONN) {
        memcpy(magic, PACKET_MAGIC.c_str(), 4);
    }
};

struct ClosePacket {
    uint8_t id;

    ClosePacket() : id(PACKET_CLOSE) {
    }
};

bool setProxy(const std::string& proxy, bool enable);
std::string createAddress(const std::string host, const std::string port);