class PowerbankUIManager {
    constructor() {
        this.deviceContainer = document.getElementById('device-container');
        this.totalDevicesElement = document.getElementById('total-devices');
        this.avgBatteryElement = document.getElementById('avg-battery');
        this.connectedDevices = {};
        this.initializeWebSocket();
    }

    initializeWebSocket() {
        this.websocket = new WebSocket('ws://localhost:8080/powerbank');
        
        this.websocket.onmessage = (event) => {
            const deviceData = JSON.parse(event.data);
            this.updateDeviceUI(deviceData);
        };
    }

    updateDeviceUI(deviceData) {
        if (!this.connectedDevices[deviceData.deviceId]) {
            this.createDeviceCard(deviceData);
        }
        
        this.updateDeviceStats(deviceData);
    }

    createDeviceCard(deviceData) {
        const card = document.createElement('div');
        card.className = 'device-card';
        card.dataset.deviceId = deviceData.deviceId;

        card.innerHTML = `
            <div class="device-header">
                <h3>${deviceData.deviceName || 'Unknown Device'}</h3>
                <span>${deviceData.deviceId}</span>
            </div>
            <div class="battery-progress">
                <div class="battery-progress-fill" style="width: 0%; background: var(--gradient-secondary)"></div>
            </div>
        `;

        this.deviceContainer.appendChild(card);
        this.connectedDevices[deviceData.deviceId] = card;
    }

    updateDeviceStats(deviceData) {
        const card = this.connectedDevices[deviceData.deviceId];
        const progressFill = card.querySelector('.battery-progress-fill');
        
        progressFill.style.width = `${deviceData.batteryLevel}%`;
        progressFill.title = `${deviceData.batteryLevel}% Battery`;

        this.recalculateOverallStats();
    }

    recalculateOverallStats() {
        const devices = Object.values(this.connectedDevices);
        const totalDevices = devices.length;
        const avgBattery = devices.reduce((sum, card) => {
            const batteryLevel = parseInt(card.querySelector('.battery-progress-fill').style.width);
            return sum + batteryLevel;
        }, 0) / totalDevices;

        this.totalDevicesElement.textContent = totalDevices;
        this.avgBatteryElement.textContent = `${avgBattery.toFixed(1)}%`;
    }
}

document.addEventListener('DOMContentLoaded', () => {
    new PowerbankUIManager();
});
