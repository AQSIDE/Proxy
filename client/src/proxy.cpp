#include <windows.h>
#include <wininet.h>
#include "proxy.h"

bool setProxy(const std::string& proxy, bool enable) {
    INTERNET_PER_CONN_OPTION_LISTA list;
    INTERNET_PER_CONN_OPTIONA options[2];

    unsigned long size = sizeof(list);

    list.dwSize = sizeof(list);
    list.pszConnection = NULL;
    list.dwOptionCount = 2;
    list.dwOptionError = 0;
    list.pOptions = options;

    options[0].dwOption = INTERNET_PER_CONN_FLAGS;
    options[0].Value.dwValue = enable ? (PROXY_TYPE_DIRECT | PROXY_TYPE_PROXY) : PROXY_TYPE_DIRECT;

    options[1].dwOption = INTERNET_PER_CONN_PROXY_SERVER;

    if (enable) {
        options[1].Value.pszValue = (LPSTR)proxy.c_str();
    }
    else {
        options[1].Value.pszValue = NULL;
    }

    BOOL result = InternetSetOptionA(NULL, INTERNET_OPTION_PER_CONNECTION_OPTION, &list, size);

    InternetSetOptionA(NULL, INTERNET_OPTION_SETTINGS_CHANGED, NULL, 0);
    InternetSetOptionA(NULL, INTERNET_OPTION_REFRESH, NULL, 0);

    return result != 0;
}

std::string createAddress(const std::string host, const std::string port) {
    return host + ":" + port;
}