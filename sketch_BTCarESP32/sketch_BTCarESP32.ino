#include <BLEDevice.h>
#include <BLEServer.h>
#include <BLEUtils.h>
#include <BLE2902.h>

// Pines del motor (puente H)
#define IN1 14
#define IN2 27
#define IN3 26
#define IN4 25

// UUIDs BLE
#define SERVICE_UUID        "0000ffe0-0000-1000-8000-00805f9b34fb"
#define CHARACTERISTIC_UUID "0000ffe1-0000-1000-8000-00805f9b34fb"

BLEServer* pServer = nullptr;
BLECharacteristic* pCharacteristic = nullptr;

enum EstadoMovimiento {
  DETENIDO,
  ADELANTE,
  ATRAS,
  IZQUIERDA,
  DERECHA
};

EstadoMovimiento estadoActual = DETENIDO;

// --------------------- FUNCIONES DE MOVIMIENTO ---------------------
void detener() {
  if (estadoActual != DETENIDO) {
    digitalWrite(IN1, LOW);
    digitalWrite(IN2, LOW);
    digitalWrite(IN3, LOW);
    digitalWrite(IN4, LOW);
    estadoActual = DETENIDO;
    Serial.println("🛑 Detenido");
  }
}

void adelante() {
  if (estadoActual != ADELANTE) {
    digitalWrite(IN1, HIGH);
    digitalWrite(IN2, LOW);
    digitalWrite(IN3, HIGH);
    digitalWrite(IN4, LOW);
    estadoActual = ADELANTE;
    Serial.println("⬆️ Adelante");
  }
}

void atras() {
  if (estadoActual != ATRAS) {
    digitalWrite(IN1, LOW);
    digitalWrite(IN2, HIGH);
    digitalWrite(IN3, LOW);
    digitalWrite(IN4, HIGH);
    estadoActual = ATRAS;
    Serial.println("⬇️ Atrás");
  }
}

void izquierda() {
  if (estadoActual != IZQUIERDA) {
    digitalWrite(IN1, LOW);
    digitalWrite(IN2, HIGH);
    digitalWrite(IN3, HIGH);
    digitalWrite(IN4, LOW);
    estadoActual = IZQUIERDA;
    Serial.println("⬅️ Izquierda");
  }
}

void derecha() {
  if (estadoActual != DERECHA) {
    digitalWrite(IN1, HIGH);
    digitalWrite(IN2, LOW);
    digitalWrite(IN3, LOW);
    digitalWrite(IN4, HIGH);
    estadoActual = DERECHA;
    Serial.println("➡️ Derecha");
  }
}

// --------------------- CALLBACKS BLE ---------------------

class CarCallback : public BLECharacteristicCallbacks {
  void onWrite(BLECharacteristic* characteristic) override {
    std::string value = characteristic->getValue();

    if (value.length() > 0) {
      Serial.print("🔘 Comandos recibidos: ");
      Serial.println(value.c_str());

      for (char cmd : value) {
        switch (cmd) {
          case 'F': adelante(); break;
          case 'B': atras(); break;
          case 'L': izquierda(); break;
          case 'R': derecha(); break;
          case 'S': detener(); break;
          default:
            Serial.print("❌ Comando desconocido: ");
            Serial.println(cmd);
            break;
        }
        delay(100); // Anti-bug: espacio entre comandos
      }
    }
  }
};

class ServerCallbacks : public BLEServerCallbacks {
  void onConnect(BLEServer* pServer) override {
    Serial.println("✅ Dispositivo conectado");
  }

  void onDisconnect(BLEServer* pServer) override {
    Serial.println("❌ Dispositivo desconectado, esperando reconexión...");
    delay(500);
    BLEDevice::startAdvertising();  // Reiniciar publicidad BLE
  }
};

// --------------------- SETUP ---------------------
void setup() {
  Serial.begin(115200);

  pinMode(IN1, OUTPUT);
  pinMode(IN2, OUTPUT);
  pinMode(IN3, OUTPUT);
  pinMode(IN4, OUTPUT);
  detener();  // Parar motores al inicio

  BLEDevice::init("ESP32_FullStackForce");
  pServer = BLEDevice::createServer();
  pServer->setCallbacks(new ServerCallbacks());

  BLEService* pService = pServer->createService(SERVICE_UUID);

  pCharacteristic = pService->createCharacteristic(
    CHARACTERISTIC_UUID,
    BLECharacteristic::PROPERTY_WRITE
  );

  pCharacteristic->setCallbacks(new CarCallback());
  pCharacteristic->addDescriptor(new BLE2902());

  pService->start();

  BLEAdvertising* pAdvertising = BLEDevice::getAdvertising();
  pAdvertising->addServiceUUID(SERVICE_UUID);
  pAdvertising->setScanResponse(false);
  pAdvertising->setMinPreferred(0x06);
  pAdvertising->setMinPreferred(0x12);
  pAdvertising->start();

  Serial.println("🚀 Esperando conexión BLE...");
}

// --------------------- LOOP ---------------------
void loop() {
  // Nada que hacer aquí por ahora
}
