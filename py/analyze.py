import pandas as pd
import numpy as np
import matplotlib.pyplot as plt
import seaborn as sns
from datetime import datetime, timedelta
import json
import os
import logging
from typing import Dict, List, Optional

class Analyzer:
    def __init__(self, data_directory: str = 'powerlog'):
        self.data_directory = data_directory
        self.setup_logging()
        self.powerbank_data = None

    def setup_logging(self):
        logging.basiConfig(
            filename='power.log',
            level=logging.INFO,
            format='%(asctime)s - %(levelname)s: %(message)s'
        )

    def load_data(self, date_range: Optional[tuple] = None)
        all_logs = []
        for filename in os.listdir(self.data_directory):
            if filename.endswith('.json'):
                filepath = os.path.join(self.data_directory, filename)
                try:
                    with open(filepath, 'r') as f:
                        logs = json.load(f)
                        all_logs.extend(logs)
                except Exception as e:
                    logging.error(f"Error reading {filename}: {e}")

       self.powerbank_data = pd.DataFrame(all_logs)
        self.powerbank_data['timestamp'] = pd.to_datetime(self.powerbank_data['timestamp'])
        
        if date_range:
            start, end = date_range
            self.powerbank_data = self.powerbank_data[
                (self.powerbank_data['timestamp'] >= start) & 
                (self.powerbank_data['timestamp'] <= end)
            ]

    def calculate_battery_metrics(self):
        metrics = {
            'total_devices': self.powerbank_data['DeviceId'].nunique(),
            'avg_battery_level': self.powerbank_data['BatteryLevel'].mean(),
            'charging_sessions': self.powerbank_data[self.powerbank_data['ChargingStatus'] == 'Charging'].groupby('DeviceId').size()
        }
        return metrics

    def analyze_charging_patterns(self):
        charging_data = self.powerbank_data[self.powerbank_data['ChargingStatus'] == 'Charging']
        device_charging_stats = charging_data.groupby('DeviceId').agg({
            'timestamp': ['min', 'max'],
            'BatteryLevel': ['min', 'max']
        })
        
        charging_sessions = []
        for device, group in charging_data.groupby('DeviceId'):
            sessions = self._detect_charging_sessions(group)
            charging_sessions.extend(sessions)
        
        return pd.DataFrame(charging_sessions)

    def _detect_charging_sessions(self, device_logs):
        sessions = []
        current_session = None
        
        for _, log in device_logs.iterrows():
            if current_session is None:
                current_session = {
                    'start_time': log['timestamp'],
                    'start_level': log['BatteryLevel'],
                    'device_id': log['DeviceId']
                }
            else:
                time_diff = (log['timestamp'] - current_session['start_time']).total_seconds()
                if time_diff > 3600:  # Session break after 1 hour
                    current_session['end_time'] = log['timestamp']
                    current_session['end_level'] = log['BatteryLevel']
                    current_session['duration'] = (current_session['end_time'] - current_session['start_time']).total_seconds()
                    sessions.append(current_session)
                    current_session = None
        
        return sessions

    def generate_battery_health_report(self):
        report = {
            'overall_battery_health': {},
            'device_specific_health': {}
        }
        
        for device in self.powerbank_data['DeviceId'].unique():
            device_logs = self.powerbank_data[self.powerbank_data['DeviceId'] == device]
            
            device_report = {
                'total_charging_cycles': len(self._detect_charging_sessions(device_logs)),
                'max_battery_level': device_logs['BatteryLevel'].max(),
                'min_battery_level': device_logs['BatteryLevel'].min(),
                'avg_charging_time': device_logs['timestamp'].max() - device_logs['timestamp'].min()
            }
            
            report['device_specific_health'][device] = device_report
        
        return report

    def visualize_battery_levels(self, output_dir: str = 'visualizations'):
        os.makedirs(output_dir, exist_ok=True)
        
        plt.figure(figsize=(15, 10))
        sns.boxplot(x='DeviceId', y='BatteryLevel', data=self.powerbank_data)
        plt.title('Battery Levels Across Devices')
        plt.xticks(rotation=45)
        plt.tight_layout()
        plt.savefig(os.path.join(output_dir, 'battery_levels_boxplot.png'))
        plt.close()

        plt.figure(figsize=(15, 10))
        for device in self.powerbank_data['DeviceId'].unique():
            device_data = self.powerbank_data[self.powerbank_data['DeviceId'] == device]
            plt.plot(device_data['timestamp'], device_data['BatteryLevel'], label=device)
        
        plt.title('Battery Level Over Time')
        plt.xlabel('Timestamp')
        plt.ylabel('Battery Level (%)')
        plt.legend()
        plt.tight_layout()
        plt.savefig(os.path.join(output_dir, 'battery_levels_timeline.png'))
        plt.close()

    def save_analysis_report(self, report_data: Dict, filename: str = 'powerbank_analysis_report.json'):
        with open(filename, 'w') as f:
            json.dump(report_data, f, indent=4, default=str)

def main():
    analyzer = PowerbankAnalyzer()
    
    try:
        analyzer.load_data(date_range=(datetime.now() - timedelta(days=30), datetime.now()))
        
        metrics = analyzer.calculate_battery_metrics()
        charging_patterns = analyzer.analyze_charging_patterns()
        health_report = analyzer.generate_battery_health_report()
        
        analyzer.visualize_battery_levels()
        
        comprehensive_report = {
            'metrics': metrics,
            'charging_patterns': charging_patterns.to_dict(),
            'health_report': health_report
        }
        
        analyzer.save_analysis_report(comprehensive_report)
        
        logging.info("Analysis completed successfully")
    
    except Exception as e:
        logging.error(f"Analysis failed: {e}")

if __name__ == "__main__":
    main()
