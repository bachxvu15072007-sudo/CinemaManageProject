using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CinemaManageProject.Models;
using Microsoft.Data.Sqlite;

namespace CinemaManageProject.ViewModels;

/// <summary>
/// Lớp nền MVVM gọn nhẹ: INotifyPropertyChanged, LoadData ảo và các helper đọc/định dạng dữ liệu dùng chung.
/// </summary>
public abstract class ViewModelBase : INotifyPropertyChanged
{
    private static readonly CultureInfo ViCulture = CultureInfo.GetCultureInfo("vi-VN");

    public event PropertyChangedEventHandler? PropertyChanged;

    public virtual void LoadData() { }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected static string FormatVnd(decimal amount) => $"{amount.ToString("N0", ViCulture)}đ";

    protected static decimal ReadDecimal(SqliteDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? 0m : Convert.ToDecimal(reader.GetValue(ordinal), CultureInfo.InvariantCulture);

    protected static string FormatSqlDateTime(string raw, string format = "dd/MM HH:mm")
        => DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out DateTime dt)
            ? dt.ToString(format)
            : raw;

    protected static string FormatPaymentMethod(string raw) => raw switch
    {
        "Cash" => "Tiền mặt",
        "BankTransfer" => "Chuyển khoản",
        "Card" => "Thẻ",
        "EWallet" => "Ví điện tử",
        _ => raw
    };

    protected static string FormatStatusLabel(string raw) => raw switch
    {
        "OpenForSale" => "Đang mở bán",
        "Scheduled" => "Đã lên lịch",
        "Completed" => "Đã chiếu",
        "Paid" => "Đã thanh toán",
        "PartiallyRefunded" => "Hoàn một phần",
        "Booked" => "Chờ soát vé",
        "CheckedIn" => "Đã check-in",
        "Cancelled" => "Đã hủy",
        _ => raw
    };

    protected static Customer? LoadCustomerProfile(SqliteConnection conn, int userId)
    {
        using SqliteCommand cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT CustomerId, FullName, Phone, Email, LoyaltyPoints, TotalSpent, Tier
            FROM Customers
            WHERE UserId = @uid
            LIMIT 1;
            """;
        cmd.Parameters.AddWithValue("@uid", userId);

        using SqliteDataReader r = cmd.ExecuteReader();
        if (!r.Read())
        {
            return null;
        }

        return new Customer
        {
            CustomerId = r.GetInt32(0),
            UserId = userId,
            FullName = r.GetString(1),
            Phone = r.GetString(2),
            Email = r.IsDBNull(3) ? null : r.GetString(3),
            LoyaltyPoints = r.GetInt32(4),
            TotalSpent = ReadDecimal(r, 5),
            Tier = Enum.TryParse(r.GetString(6), true, out CustomerTier tier) ? tier : CustomerTier.Member
        };
    }
}

/// <summary>
/// ICommand gọn nhẹ dùng trong MVVM binding.
/// </summary>
public sealed class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Predicate<object?>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
        : this(_ => execute(), canExecute is null ? null : _ => canExecute())
    {
    }

    public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

    public void Execute(object? parameter) => _execute(parameter);
}
