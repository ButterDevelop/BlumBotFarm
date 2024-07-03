import pandas as pd

# Путь к исходному CSV файлу
input_csv_file = 'accounts.csv'

# Путь к выходному CSV файлу
output_csv_file = 'output.csv'

def isNaN(value):
    # Функция для проверки на NaN (Not a Number) или None
    return value != value

def format_add_account(row):
    # Форматирование команды /addaccount
    if (pd.notna(row['Telegram Name']) and 
        pd.notna(row['Access Token']) and 
        pd.notna(row['Refresh Token']) and 
        pd.notna(row['Proxy'])):
        
        telegram_name = str(row['Telegram Name']).strip()
        access_token = str(row['Access Token']).strip()
        refresh_token = str(row['Refresh Token']).strip()
        proxy = str(row['Proxy']).strip()
        
        return f"/addaccount {telegram_name} {access_token} {refresh_token} -120 socks5://{proxy}"
    else:
        return ""

def format_provider_token(row):
    # Форматирование команды /providertoken
    if (pd.notna(row['Telegram Name']) and 
        pd.notna(row['Auth TG Query Link'])):
        
        telegram_name = str(row['Telegram Name']).strip()
        auth_tg_query_link = str(row['Auth TG Query Link']).strip()
        
        return f"/providertoken {telegram_name} {auth_tg_query_link}"
    else:
        return ""

def process_csv_to_csv(input_file, output_file):
    # Чтение CSV файла
    df = pd.read_csv(input_file)

    # Применение форматирования к колонкам
    df['Telegram Add Command'] = df.apply(format_add_account, axis=1)
    df['Telegram Provider Token'] = df.apply(format_provider_token, axis=1)

    # Сохранение данных в CSV файл без лишних символов
    df.to_csv(output_file, index=False, lineterminator='\n')

    print(f'Данные успешно записаны в файл: {output_file}')

# Запуск функции для обработки CSV и записи в CSV
process_csv_to_csv(input_csv_file, output_csv_file)
