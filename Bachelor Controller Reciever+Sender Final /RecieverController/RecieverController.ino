#include <M5Unified.h>
#include <WiFi.h>
#include <esp_now.h>
#include <esp_wifi.h>
#include <esp_mac.h>
#include <esp_log.h>

#define ESPNOW_CHANNEL 1

typedef struct __attribute__((packed)) {
  char     controllerName[16];
  int32_t  rotationValue;
  uint8_t  pushed;
} ControllerPacket;

const uint8_t PACKET_HEADER_1 = 0xAA;
const uint8_t PACKET_HEADER_2 = 0x55;

struct ControllerSlot {
  bool             active;
  uint8_t          mac[6];
  ControllerPacket pkt;
  uint32_t         count;
};

// Shared between ESP-NOW callback and loop()
volatile bool   hasNewPacket = false;
ControllerSlot  slots[2] = {};

void printMacAddress() {
  uint8_t mac[6];
  esp_read_mac(mac, ESP_MAC_WIFI_STA);

  char macStr[18];
  snprintf(macStr, sizeof(macStr), "%02X:%02X:%02X:%02X:%02X:%02X",
           mac[0], mac[1], mac[2], mac[3], mac[4], mac[5]);

  M5.Display.setCursor(0, 0);
  M5.Display.setTextColor(TFT_CYAN, TFT_BLACK);
  M5.Display.println("MAC:");
  M5.Display.setTextColor(TFT_WHITE, TFT_BLACK);
  M5.Display.println(macStr);
  M5.Display.drawFastHLine(0, M5.Display.fontHeight() * 2 + 2,
                           M5.Display.width(), TFT_DARKGREY);
}

void drawSlot(int idx, int y) {
  const int w    = M5.Display.width();
  const int rowH = M5.Display.fontHeight();
  M5.Display.fillRect(0, y, w, rowH * 4, TFT_BLACK);

  M5.Display.setCursor(0, y);
  M5.Display.setTextColor(idx == 0 ? TFT_GREEN : TFT_YELLOW, TFT_BLACK);
  M5.Display.printf("C%d:", idx + 1);

  if (!slots[idx].active) {
    M5.Display.setTextColor(TFT_DARKGREY, TFT_BLACK);
    M5.Display.println(" --");
    return;
  }

  M5.Display.setTextColor(TFT_WHITE, TFT_BLACK);
  M5.Display.printf(" %s\n", slots[idx].pkt.controllerName);
  M5.Display.printf("  Rot:%ld\n", (long)slots[idx].pkt.rotationValue);
  M5.Display.printf("  Btn:%u  N:%lu\n",
                    slots[idx].pkt.pushed,
                    (unsigned long)slots[idx].count);
}

void drawAll() {
  const int rowH  = M5.Display.fontHeight();
  const int yTop  = rowH * 2 + 6;          // below the MAC header
  const int slotH = rowH * 4 + 2;          // 4 text lines + a little padding
  drawSlot(0, yTop);
  M5.Display.drawFastHLine(0, yTop + slotH - 1,
                           M5.Display.width(), TFT_DARKGREY);
  drawSlot(1, yTop + slotH);
}

void onReceive(const esp_now_recv_info_t *recvInfo,
               const uint8_t *incomingData, int len) {
  if (len != sizeof(ControllerPacket)) return;

  ControllerPacket pkt;
  memcpy(&pkt, incomingData, sizeof(pkt));

  // ── Build the entire frame in one local buffer and send it with a
  //    single Serial.write() call. This is atomic from the UART driver's
  //    point of view — nothing can sneak bytes into the middle of the
  //    packet (no preemption, no buffer-fill blocking between bytes,
  //    no rogue ESP-IDF log output, no interleaving).
  //
  //    Frame layout (24 bytes):
  //      [0]   0xAA           header byte 1
  //      [1]   0x55           header byte 2
  //      [2..22] payload      ControllerPacket (21 bytes, packed)
  //      [23]  XOR checksum   xor of all 21 payload bytes
  // ────────────────────────────────────────────────────────────────────
  uint8_t frame[2 + sizeof(ControllerPacket) + 1];
  frame[0] = PACKET_HEADER_1;
  frame[1] = PACKET_HEADER_2;
  memcpy(&frame[2], &pkt, sizeof(pkt));

  uint8_t cksum = 0;
  const uint8_t *p = (const uint8_t*)&pkt;
  for (size_t i = 0; i < sizeof(pkt); i++) cksum ^= p[i];
  frame[2 + sizeof(pkt)] = cksum;

  Serial.write(frame, sizeof(frame));

  // ── Display bookkeeping ─────────────────────────────────────────────
  // Make sure the name is null-terminated before we scan it
  char nameBuf[sizeof(pkt.controllerName) + 1];
  memcpy(nameBuf, pkt.controllerName, sizeof(pkt.controllerName));
  nameBuf[sizeof(pkt.controllerName)] = '\0';

  // Decide slot from the controller name: "1" -> top, "2" -> bottom
  int idx = -1;
  if (strchr(nameBuf, '2'))      idx = 1;
  else if (strchr(nameBuf, '1')) idx = 0;
  else return; // unknown controller, ignore for display

  slots[idx].active = true;
  memcpy(slots[idx].mac, recvInfo->src_addr, 6);
  slots[idx].pkt    = pkt;
  slots[idx].count++;
  hasNewPacket = true;
}


void setup() {
  // Suppress all ESP-IDF logging — it shares UART0 with our binary stream,
  // and any "I (1234) wifi: ..." line would corrupt the packet flow.
  esp_log_level_set("*", ESP_LOG_NONE);

  auto cfg = M5.config();
  M5.begin(cfg);
  M5.Display.setRotation(0);
  M5.Display.fillScreen(TFT_BLACK);
  M5.Display.setTextSize(1);
  M5.Display.setTextColor(TFT_WHITE, TFT_BLACK);

  Serial.begin(115200);
  delay(500);

  WiFi.mode(WIFI_STA);
  esp_wifi_start();
  esp_wifi_set_channel(ESPNOW_CHANNEL, WIFI_SECOND_CHAN_NONE);

  printMacAddress();
  drawAll();  // show empty slots until packets arrive

  if (esp_now_init() != ESP_OK) {
    M5.Display.setTextColor(TFT_RED, TFT_BLACK);
    M5.Display.println("ESP-NOW init failed");
    while (true) delay(100);
  }

  esp_now_register_recv_cb(onReceive);
}

void loop() {
  if (hasNewPacket) {
    hasNewPacket = false;
    drawAll();
  }
  delay(10);
}

