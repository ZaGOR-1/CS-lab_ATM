using System;
using System.Collections.Generic;
using System.Linq;

namespace AtmLab
{
    public class AtmEventArgs : EventArgs
    {
        public string Message { get; }
        public bool IsSuccess { get; }

        public AtmEventArgs(string message, bool isSuccess = true)
        {
            Message = message;
            IsSuccess = isSuccess;
        }
    }

    public class Account
    {
        public string CardNumber { get; private set; }
        public string PinCode { get; private set; }
        public string FullName { get; private set; }
        public decimal Balance { get; private set; }

        public Account(string cardNum, string pin, string name, decimal initialBalance)
        {
            CardNumber = cardNum;
            PinCode = pin;
            FullName = name;
            Balance = initialBalance;
        }

        public void Withdraw(decimal amount)
        {
            Balance -= amount;
        }

        public void Deposit(decimal amount)
        {
            Balance += amount;
        }
    }

    public class Bank
    {
        public string Name { get; set; }
        private List<Account> _accounts = new List<Account>();

        public Bank(string name)
        {
            Name = name;
        }

        public void AddAccount(Account acc)
        {
            _accounts.Add(acc);
        }

        public Account FindAccount(string cardNum)
        {
            return _accounts.FirstOrDefault(a => a.CardNumber == cardNum);
        }
    }

    public class AutomatedTellerMachine
    {
        public string Id { get; set; }
        public string Address { get; set; }
        public decimal CashInAtm { get; private set; }

        private Bank _bank;
        private Account _currentAccount;

        public event EventHandler<AtmEventArgs> OnAuthentication;
        public event EventHandler<AtmEventArgs> OnBalanceView;
        public event EventHandler<AtmEventArgs> OnWithdrawal;
        public event EventHandler<AtmEventArgs> OnDeposit;
        public event EventHandler<AtmEventArgs> OnTransfer;

        public AutomatedTellerMachine(string id, string addr, decimal initialCash, Bank bank)
        {
            Id = id;
            Address = addr;
            CashInAtm = initialCash;
            _bank = bank;
        }

        public void Authenticate(string cardNum, string pin)
        {
            var account = _bank.FindAccount(cardNum);

            if (account != null && account.PinCode == pin)
            {
                _currentAccount = account;
                OnAuthentication?.Invoke(this, new AtmEventArgs($"Вітаємо, {account.FullName}! Ви увійшли в систему."));
            }
            else
            {
                OnAuthentication?.Invoke(this, new AtmEventArgs("Помилка: Невірний номер картки або ПІН-код.", false));
            }
        }

        public void ShowBalance()
        {
            if (!CheckAuth()) return;
            OnBalanceView?.Invoke(this, new AtmEventArgs($"На вашому рахунку: {_currentAccount.Balance} грн."));
        }

        public void WithdrawMoney(decimal amount)
        {
            if (!CheckAuth()) return;

            if (amount <= 0)
            {
                OnWithdrawal?.Invoke(this, new AtmEventArgs("Сума має бути більше нуля.", false));
                return;
            }

            if (amount > _currentAccount.Balance)
            {
                OnWithdrawal?.Invoke(this, new AtmEventArgs("Операцію відхилено: Недостатньо коштів на картці.", false));
                return;
            }

            if (amount > CashInAtm)
            {
                OnWithdrawal?.Invoke(this, new AtmEventArgs("Вибачте, у банкоматі закінчилась готівка.", false));
                return;
            }

            _currentAccount.Withdraw(amount);
            CashInAtm -= amount;
            OnWithdrawal?.Invoke(this, new AtmEventArgs($"Видано {amount} грн. Залишок: {_currentAccount.Balance} грн."));
        }

        public void DepositMoney(decimal amount)
        {
            if (!CheckAuth()) return;

            if (amount <= 0)
            {
                OnDeposit?.Invoke(this, new AtmEventArgs("Сума має бути більше нуля.", false));
                return;
            }

            _currentAccount.Deposit(amount);
            CashInAtm += amount;
            OnDeposit?.Invoke(this, new AtmEventArgs($"Рахунок поповнено на {amount} грн. Баланс: {_currentAccount.Balance} грн."));
        }

        public void TransferMoney(string targetCardNum, decimal amount)
        {
            if (!CheckAuth()) return;

            if (targetCardNum == _currentAccount.CardNumber)
            {
                OnTransfer?.Invoke(this, new AtmEventArgs("Неможливо здійснити переказ на власну картку.", false));
                return;
            }

            var targetAccount = _bank.FindAccount(targetCardNum);

            if (targetAccount == null)
            {
                OnTransfer?.Invoke(this, new AtmEventArgs("Помилка: Картку отримувача не знайдено в базі.", false));
                return;
            }

            if (_currentAccount.Balance < amount)
            {
                OnTransfer?.Invoke(this, new AtmEventArgs("Помилка: Недостатньо коштів для переказу.", false));
                return;
            }

            _currentAccount.Withdraw(amount);
            targetAccount.Deposit(amount);

            OnTransfer?.Invoke(this, new AtmEventArgs($"Успішно переказано {amount} грн на картку {targetCardNum}."));
        }

        private bool CheckAuth()
        {
            if (_currentAccount == null)
            {
                OnAuthentication?.Invoke(this, new AtmEventArgs("Спочатку авторизуйтесь (введіть номер та ПІН)!", false));
                return false;
            }
            return true;
        }
    }

    public class AtmConsoleUI
    {
        private AutomatedTellerMachine _atm;

        public AtmConsoleUI(AutomatedTellerMachine atm)
        {
            _atm = atm;
            _atm.OnAuthentication += (s, e) => ShowMessage("Авторизація", e.Message, e.IsSuccess);
            _atm.OnBalanceView += (s, e) => ShowMessage("Баланс", e.Message, true);
            _atm.OnWithdrawal += (s, e) => ShowMessage("Зняття готівки", e.Message, e.IsSuccess);
            _atm.OnDeposit += (s, e) => ShowMessage("Поповнення", e.Message, true);
            _atm.OnTransfer += (s, e) => ShowMessage("Переказ коштів", e.Message, e.IsSuccess);
        }

        public static void ShowMessage(string title, string text, bool isSuccess)
        {
            if (isSuccess)
                Console.WriteLine($"✅ [{title}] {text}");
            else
                Console.WriteLine($"❌ [{title}] {text}");
        }

        class Program
        {
            [STAThread]
            static void Main(string[] args)
            {
                Console.OutputEncoding = System.Text.Encoding.UTF8;
                Console.Title = "Лабораторна робота №1 - Банкомат";

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("==================================================");
                Console.WriteLine("            Лабораторна робота №1");
                Console.WriteLine(" Тема: Використання делегатів та подій у C#");
                Console.WriteLine("--------------------------------------------------");
                Console.WriteLine(" Виконав: Загоровський Денис студент ЗІПЗ-24-1");
                Console.WriteLine("==================================================\n");
                Console.ResetColor();

                Bank miyBank = new Bank("ПриватБанк");

                const string card1Number = "1111";
                const string card1Pin = "0000";
                const string card1Name = "Загоровський Денис";
                const decimal card1Balance = 2000;

                const string card2Number = "2222";
                const string card2Pin = "1234";
                const string card2Name = "Петро Викладач";
                const decimal card2Balance = 15000;

                miyBank.AddAccount(new Account(card1Number, card1Pin, card1Name, card1Balance));
                miyBank.AddAccount(new Account(card2Number, card2Pin, card2Name, card2Balance));

                AutomatedTellerMachine atm = new AutomatedTellerMachine("ATM-001", "Київ, Хрещатик", 50000, miyBank);

                AtmConsoleUI ui = new AtmConsoleUI(atm);

                while (true)
                {
                    Console.WriteLine("\n--- ГОЛОВНЕ МЕНЮ ---");
                    Console.WriteLine("1. Вставити картку (Вхід)");
                    Console.WriteLine("2. Перевірити баланс");
                    Console.WriteLine("3. Зняти готівку");
                    Console.WriteLine("4. Поповнити картку");
                    Console.WriteLine("5. Переказати кошти");
                    Console.WriteLine("0. Вихід");
                    Console.Write("Ваш вибір > ");

                    string choice = Console.ReadLine();

                    switch (choice)
                    {
                        case "1":
                            Console.Write("Введіть номер картки: ");
                            string cardNum = Console.ReadLine();
                            Console.Write("Введіть ПІН-код: ");
                            string pinKod = Console.ReadLine();
                            atm.Authenticate(cardNum, pinKod);
                            break;

                        case "2":
                            atm.ShowBalance();
                            break;

                        case "3":
                            Console.Write("Введіть суму для зняття: ");
                            if (decimal.TryParse(Console.ReadLine(), out decimal sumaZnyattya))
                            {
                                atm.WithdrawMoney(sumaZnyattya);
                            }
                            else
                            {
                                Console.WriteLine("❗ Будь ласка, введіть коректне число.");
                            }
                            break;

                        case "4":
                            Console.Write("Введіть суму поповнення: ");
                            if (decimal.TryParse(Console.ReadLine(), out decimal sumaPopovnennya))
                            {
                                atm.DepositMoney(sumaPopovnennya);
                            }
                            else
                            {
                                Console.WriteLine("❗ Помилка вводу суми.");
                            }
                            break;

                        case "5":
                            Console.Write("Номер картки отримувача: ");
                            string targetCard = Console.ReadLine();
                            Console.Write("Сума переказу: ");
                            if (decimal.TryParse(Console.ReadLine(), out decimal sumaPerekazu))
                            {
                                atm.TransferMoney(targetCard, sumaPerekazu);
                            }
                            else
                            {
                                Console.WriteLine("❗ Помилка вводу суми.");
                            }
                            break;

                        case "0":
                            Console.WriteLine("Завершення роботи...");
                            return;

                        default:
                            Console.WriteLine("Невідома команда. Спробуйте ще раз.");
                            break;
                    }
                }
            }
        }
    }
}
