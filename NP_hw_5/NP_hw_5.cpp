#include <iostream>

#define _WIN32_WINNT 0x600
#include <WinSock2.h>   
#include <ws2tcpip.h>
#pragma comment (lib, "ws2_32.lib")   
#include <Windows.h>
#include <stdexcept>

#include <fstream>

using namespace std;

const int PORT = 2024;

int main() {
    try {
        WSADATA wsaData;
        if (WSAStartup(MAKEWORD(2, 2), &wsaData) != 0)
            throw runtime_error("WSAStartup failed");

        // socket
        SOCKET server = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
        if (server == INVALID_SOCKET)
            throw runtime_error("socket failed");

        sockaddr_in addr;
        addr.sin_family = AF_INET;
        inet_pton(AF_INET, "127.0.0.1", &addr.sin_addr);
        addr.sin_port = htons(PORT);


        if (connect(server, (const sockaddr*)&addr, sizeof(addr)) == SOCKET_ERROR)
            throw runtime_error("connect failed");

        ofstream file("file.txt", ios::out | ios::binary);

        char buffer[1024];

        int pos = 0, length = sizeof(file);
        char content[sizeof(file)];

        while (pos < length) {

            fd_set check;
            FD_ZERO(&check);
            FD_SET(server, &check);

            fd_set readable = check, writable = check, inError = check;
            TIMEVAL wait{ 0, 0 };
            if (select(FD_SETSIZE, &readable, &writable, &inError, &wait) == SOCKET_ERROR)
                throw runtime_error("select failed");

            if (FD_ISSET(server, &readable)) {  
                char buffer[100];
                int read = recv(server, buffer, sizeof(buffer), 0);
                if (read < 0)
                    throw runtime_error("recv failed");
                else if (read == 0) {  
                    cout << "Server disconnected" << endl;
                    break;
                }
                cout << buffer << endl;
            }

            int read = recv(server, buffer + pos, length - pos, 0);
  
            for (int i = 0; i < sizeof(file); i++)
                file << content[i];
                                      
            if (!file)
                throw runtime_error("recv failed");

            else if (read == 0)
                throw runtime_error("disconnected from server");
            pos += read;
        }
        cout << content;

    }
    catch (runtime_error& e) {
        cerr << e.what() << endl;
    }
}
