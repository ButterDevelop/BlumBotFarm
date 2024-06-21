import random
import string
from openpyxl import Workbook

def generate_random_password(length=8):
    characters = string.ascii_letters + string.digits + string.punctuation
    password = ''.join(random.choice(characters) for i in range(length))
    return password

def create_excel_file(filename):
    # Создание книги и листа
    wb = Workbook()
    ws = wb.active
    ws.title = "Data"

    # Заголовки колонок
    columns = ["№", "Phone Number", "Password", "Cloud Password", "Username", "Access Token", "Refresh Token", "Proxy"]
    ws.append(columns)

    # Генерация 30 строк данных
    for i in range(1, 31):
        row = [
            i,  # Номер колонки
            "",  # Phone Number
            generate_random_password(),  # Password
            generate_random_password(),  # Cloud Password
            "",  # Username
            "",  # Access Token
            "",  # Refresh Token
            ""   # Proxy
        ]
        ws.append(row)

    # Сохранение файла
    wb.save(filename)
    print(f"Файл {filename} успешно создан.")

if __name__ == "__main__":
    create_excel_file("generated_data.xlsx")
