# 学习站用的设备仿真：一个镜像里装好 Modbus（pymodbus）和西门子 S7（python-snap7）的模拟服务端
FROM python:3.12-slim
RUN pip install --no-cache-dir "pymodbus==3.16.2" "python-snap7==3.4.0"
WORKDIR /app
COPY modbus_server.py s7_server.py ./
