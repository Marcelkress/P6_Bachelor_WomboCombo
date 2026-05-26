// AI DISCLAIMER
// This sketch has been made with help from AI 

#include <FastLED.h>
#include <ESP32Encoder.h>
#include <Wire.h>
#include <Adafruit_DRV2605.h>
#include <WiFi.h>
#include <esp_now.h>
#include <esp_wifi.h>

Adafruit_DRV2605 drv;

#define LED_PIN         4
#define NUM_LEDS        69
#define ENC_A           1
#define ENC_B           2
#define BUTTON_PIN      7
#define WINDOW_SIZE     5
#define COUNTS_PER_REV  30
#define ESPNOW_CHANNEL  1
#define LEDMoon_Place   51
#define LEDStar_Place   18


// Old Mac address on the stampS3 was 0x30, 0xED, 0xA0, 0xCA, 0x00, 0x50

static uint8_t receiverMac[] = { 0xCC, 0xBA, 0x97, 0x03, 0x48, 0xD4 };

typedef struct __attribute__((packed)) {
  char     controllerName[16];
  int32_t  rotationValue;
  uint8_t  pushed;
} ControllerPacket;

CRGB leds[NUM_LEDS];
ESP32Encoder encoder;

int  lastValue    = 0;
long lastRawCount = 0;

bool flashActive = false;
unsigned long flashStartTime = 0;
const unsigned long flashDuration = 1000;

bool lastButtonReading = HIGH;
bool buttonState = HIGH;
unsigned long lastDebounceTime = 0;
const unsigned long debounceDelay = 20;

unsigned long buttonHoldStart = 0;
bool buttonHeld = false;
const unsigned long calibrationHoldTime = 5000;

volatile bool sendBusy = false;

/*
bool moon_star = false
bool moon_moon = false 
bool moon_sun = false 
bool star_star = false 
bool star_moon = false 
bool star_sun = false   
*/

// ── Send helper ──────────────────────────────────────────────────────────────
void sendPacket(int32_t rotationValue, uint8_t pushed) {
  if (sendBusy) return;
  sendBusy = true;

  ControllerPacket pkt = {};
  strncpy(pkt.controllerName, "Controller 2", sizeof(pkt.controllerName) - 1);
  pkt.rotationValue = rotationValue;
  pkt.pushed = pushed;
  esp_now_send(receiverMac, (uint8_t *)&pkt, sizeof(pkt));
}

void onSent(const wifi_tx_info_t *tx_info, esp_now_send_status_t status) {
  sendBusy = false;
}

// ── ESP-NOW init ─────────────────────────────────────────────────────────────
void espNowInit() {
  WiFi.mode(WIFI_STA);
  esp_wifi_start();
  esp_wifi_set_channel(ESPNOW_CHANNEL, WIFI_SECOND_CHAN_NONE);

  Serial.printf("Sender MAC: %s (ch=%d)\n", WiFi.macAddress().c_str(), ESPNOW_CHANNEL);

  if (esp_now_init() != ESP_OK) {
    Serial.println("ESP-NOW init failed");
    while (true) delay(100);
  }

  esp_now_register_send_cb(onSent);

  esp_now_peer_info_t peer = {};
  memcpy(peer.peer_addr, receiverMac, 6);
  peer.channel = ESPNOW_CHANNEL;
  peer.encrypt = false;

  if (esp_now_add_peer(&peer) != ESP_OK) {
    Serial.println("Failed to add peer");
    while (true) delay(100);
  }
}

// ── LED / haptic helpers ─────────────────────────────────────────────────────
bool isMoonCorrect(int v) {
  return
    // Moon on top — Moon-Star
       (  v == 4
       || v == 5
       || v == 6
      
    // Moon on top — Moon-Moon
       || v == 29
       || v == 0
       || v == 1
       
       
    // Moon on top — Moon-Sun
       || v == 23
       || v == 24
       || v == 25
       || v == 26
       );
}

bool isStarCorrect(int v) {
  return
    // Star on top — Star-Moon
       (  v == 13
       || v == 14
       || v == 15
      
    // Star on top — Star-Star
       || v == 18
       || v == 19
       || v == 20
    // Star on top — Star-Sun
       || v == 8
       || v == 9
       || v == 10
      
       );
}



void startFlash() {
  flashActive = true;
  flashStartTime = millis();
}

void vibrateTick() {
  drv.setWaveform(0, 47);
  drv.setWaveform(1, 0);
  drv.go();
}

// ── Setup ────────────────────────────────────────────────────────────────────
void setup() {
  Serial.begin(115200);

  FastLED.addLeds<WS2812, LED_PIN, GRB>(leds, NUM_LEDS);
  FastLED.setBrightness(255);
  FastLED.clear();
  FastLED.show();

  pinMode(ENC_A, INPUT_PULLUP);
  pinMode(ENC_B, INPUT_PULLUP);
  pinMode(BUTTON_PIN, INPUT_PULLUP);

  ESP32Encoder::useInternalWeakPullResistors = puType::up;
  encoder.attachHalfQuad(ENC_A, ENC_B);
  encoder.setCount(0);

  Wire.begin();
  if (!drv.begin()) {
    Serial.println("DRV2605 not found");
    while (1);
  }

  drv.useERM();
  drv.selectLibrary(5);
  drv.setMode(DRV2605_MODE_INTTRIG);

  espNowInit();
}

// ── Loop ─────────────────────────────────────────────────────────────────────
void loop() {

  // ── Button debounce and press/hold detection ─────────────────────────────
  bool reading = digitalRead(BUTTON_PIN);

  if (reading != lastButtonReading) {
    lastDebounceTime = millis();
    lastButtonReading = reading;
  }

  if ((millis() - lastDebounceTime) > debounceDelay) {
    if (reading != buttonState) {
      buttonState = reading;

      if (buttonState == LOW) {
        // Button just pressed — start tracking hold
        buttonHoldStart = millis();
        buttonHeld = false;
      } else if (!buttonHeld) {
        // Button released before 5s — normal short press
        startFlash();
        vibrateTick();
        sendPacket(lastValue, 1);
      }
    }
  }

  // ── Calibration: held for 5 seconds ──────────────────────────────────────
  if (buttonState == LOW && !buttonHeld &&
      (millis() - buttonHoldStart) >= calibrationHoldTime) {
    buttonHeld = true;
    encoder.setCount(0);
    lastRawCount = 0;
    lastValue = 0;
    Serial.println("Calibrated! Encoder reset to 0.");

    // Visual feedback — flash green
    fill_solid(leds, NUM_LEDS, CRGB::Green);
    FastLED.setBrightness(255);
    FastLED.show();
    delay(300);

    sendPacket(0, 0);
  }

  // ── Read encoder ─────────────────────────────────────────────────────────
  long rawCount = encoder.getCount();

  int positionInRev = rawCount % COUNTS_PER_REV;
  if (positionInRev < 0) positionInRev += COUNTS_PER_REV;

  int stepValue = positionInRev;

  if (rawCount != lastRawCount) {
    Serial.printf("rawCount: %ld  |  stepValue: %d\n", rawCount, stepValue);
    lastRawCount = rawCount;
    lastValue = stepValue;
    sendPacket(stepValue, 0);
  }

  // ── Flash animation (LED-only, no longer blocks encoder/send) ────────────
  if (flashActive) {
    unsigned long elapsed = millis() - flashStartTime;
    if (elapsed >= flashDuration) {
      flashActive = false;
    } else {
      uint8_t brightness = map(elapsed, 0, flashDuration, 50, 0);
      bool isValid = isMoonCorrect(stepValue) || isStarCorrect(stepValue);
  
      if (isValid) {
        fill_solid(leds, NUM_LEDS, CRGB::Green);
        Serial.println("✓ Valid position! Flashing GREEN");
      } else {
        fill_solid(leds, NUM_LEDS, CRGB::Red);
        Serial.println("✗ Invalid position! Flashing RED");
      }
      FastLED.setBrightness(brightness);
      FastLED.show();
      return;
    }
  }

    // ── LEDs always update every loop ────────────────────────────────────────
  int ledIndex = map(stepValue, 0, COUNTS_PER_REV - 1, 0, NUM_LEDS - 1);

  CRGB moonColor = isMoonCorrect(stepValue) ? CRGB::Green : CRGB::Red;
  CRGB starColor = isStarCorrect(stepValue) ? CRGB::Green : CRGB::Red;

  fill_solid(leds, NUM_LEDS, CRGB::Black);

  for (int i = 0; i < WINDOW_SIZE; i++) {
    leds[(ledIndex + LEDStar_Place + i) % NUM_LEDS] = starColor;
  }

  for (int i = 0; i < WINDOW_SIZE; i++) {
    leds[(ledIndex + LEDMoon_Place + i) % NUM_LEDS] = moonColor;
  }

  FastLED.setBrightness(255);
  FastLED.show();

}
