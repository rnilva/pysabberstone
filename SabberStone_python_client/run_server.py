import os
import subprocess


def run_server():
    os.system("dotnet run -p ../SabberStone_gRPC/. &")


def run_server_sub():
    print("Starting SabberStone Server.....")
    subprocess.Popen(["dotnet", "run", "--project" "../SabberStone_gRPC/"])
