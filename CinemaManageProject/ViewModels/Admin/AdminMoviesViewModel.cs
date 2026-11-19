using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Windows.Media;
using CinemaManageProject.Data;
using CinemaManageProject.Helpers;

namespace CinemaManageProject.ViewModels.Admin;

public sealed record MovieStatusOption(string Value, string Label)
{
    public override string ToString() => Label;
}

public sealed record GenreFilterOption(int? GenreId, string Name)
{
    public override string ToString() => Name;
}

public sealed class GenreSelectionItem : ViewModelBase
{
    private bool _isSelected;

    public GenreSelectionItem(int genreId, string name) { GenreId = genreId; Name = name; }

    public int GenreId { get; }
    public string Name { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; OnPropertyChanged(nameof(IsSelected)); }
    }
}

/// <summary>
/// ViewModel Quản lý Phim &amp; Thể loại cho vai trò Admin.
/// </summary>
public sealed class AdminMoviesViewModel : ViewModelBase
{
    private static readonly string[] DateFormats = ["dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd"];

    private readonly List<AdminMovieRow> _allMovies = [];
    private int? _editingMovieId;
    private string? _posterPath;
    private string? _pendingPosterSourcePath;

    private string _searchKeyword = string.Empty;
    private GenreFilterOption? _selectedGenreFilter;
    private string _selectedAgeRatingFilter = "Tất cả";
    private MovieStatusOption? _selectedStatusFilterOption;
    private string _resultCountText = "0 phim";
    private AdminMovieRow? _selectedMovie;

    private string _title = string.Empty;
    private string _originalTitle = string.Empty;
    private string _director = string.Empty;
    private string _cast = string.Empty;
    private string _durationText = string.Empty;
    private string _language = "Phụ đề Tiếng Việt";
    private string _releaseDateText = DateTime.Today.ToString("dd/MM/yyyy");
    private string _endDateText = string.Empty;
    private string _selectedAgeRating = "P";
    private MovieStatusOption? _selectedStatusOption;
    private string _newGenreName = string.Empty;
    private string _description = string.Empty;
    private ImageSource? _posterPreview;
    private string _posterFileName = "Chưa chọn ảnh (JPG, PNG)";
    private string? _errorMessage;
    private string? _successMessage;

    public AdminMoviesViewModel()
    {
        AgeRatingOptions = ["P", "K", "T13", "T16", "T18"];
        AgeRatingFilterOptions = ["Tất cả", "P", "K", "T13", "T16", "T18"];
        StatusOptions =
        [
            new MovieStatusOption("NowShowing", "Đang chiếu"),
            new MovieStatusOption("ComingSoon", "Sắp chiếu"),
            new MovieStatusOption("Ended", "Ngừng chiếu")
        ];
        StatusFilterOptions =
        [
            new MovieStatusOption("All", "Tất cả"),
            new MovieStatusOption("NowShowing", "Đang chiếu"),
            new MovieStatusOption("ComingSoon", "Sắp chiếu"),
            new MovieStatusOption("Ended", "Ngừng chiếu")
        ];

        _selectedStatusOption = StatusOptions[0];
        _selectedStatusFilterOption = StatusFilterOptions[0];
        LoadData();
    }

    public IReadOnlyList<string> AgeRatingOptions { get; }
    public IReadOnlyList<string> AgeRatingFilterOptions { get; }
    public IReadOnlyList<MovieStatusOption> StatusOptions { get; }
    public IReadOnlyList<MovieStatusOption> StatusFilterOptions { get; }

    public ObservableCollection<AdminMovieRow> Movies { get; } = [];
    public ObservableCollection<GenreSelectionItem> GenreOptions { get; } = [];
    public ObservableCollection<GenreFilterOption> GenreFilterOptions { get; } = [];

    public string SearchKeyword
    {
        get => _searchKeyword;
        set { if (_searchKeyword == value) return; _searchKeyword = value; OnPropertyChanged(nameof(SearchKeyword)); ApplyFilter(); }
    }

    public GenreFilterOption? SelectedGenreFilter
    {
        get => _selectedGenreFilter;
        set { if (Equals(_selectedGenreFilter, value)) return; _selectedGenreFilter = value; OnPropertyChanged(nameof(SelectedGenreFilter)); ApplyFilter(); }
    }

    public string SelectedAgeRatingFilter
    {
        get => _selectedAgeRatingFilter;
        set { if (_selectedAgeRatingFilter == value) return; _selectedAgeRatingFilter = value; OnPropertyChanged(nameof(SelectedAgeRatingFilter)); ApplyFilter(); }
    }

    public MovieStatusOption? SelectedStatusFilterOption
    {
        get => _selectedStatusFilterOption;
        set { if (Equals(_selectedStatusFilterOption, value)) return; _selectedStatusFilterOption = value; OnPropertyChanged(nameof(SelectedStatusFilterOption)); ApplyFilter(); }
    }

    public AdminMovieRow? SelectedMovie
    {
        get => _selectedMovie;
        set { _selectedMovie = value; OnPropertyChanged(nameof(SelectedMovie)); if (value is not null) PopulateForm(value); }
    }

    public string ResultCountText { get => _resultCountText; private set { _resultCountText = value; OnPropertyChanged(nameof(ResultCountText)); } }
    public bool IsEditMode => _editingMovieId.HasValue;
    public string FormHeaderText => IsEditMode ? "Chỉnh sửa phim" : "Thêm phim mới";
    public string FormModeBadge => IsEditMode ? $"MÃ #{_editingMovieId}" : "THÊM MỚI";

    public string Title { get => _title; set { _title = value; OnPropertyChanged(nameof(Title)); } }
    public string OriginalTitle { get => _originalTitle; set { _originalTitle = value; OnPropertyChanged(nameof(OriginalTitle)); } }
    public string Director { get => _director; set { _director = value; OnPropertyChanged(nameof(Director)); } }
    public string Cast { get => _cast; set { _cast = value; OnPropertyChanged(nameof(Cast)); } }
    public string DurationText { get => _durationText; set { _durationText = value; OnPropertyChanged(nameof(DurationText)); } }
    public string Language { get => _language; set { _language = value; OnPropertyChanged(nameof(Language)); } }
    public string ReleaseDateText { get => _releaseDateText; set { _releaseDateText = value; OnPropertyChanged(nameof(ReleaseDateText)); } }
    public string EndDateText { get => _endDateText; set { _endDateText = value; OnPropertyChanged(nameof(EndDateText)); } }
    public string SelectedAgeRating { get => _selectedAgeRating; set { _selectedAgeRating = value; OnPropertyChanged(nameof(SelectedAgeRating)); } }
    public MovieStatusOption? SelectedStatusOption { get => _selectedStatusOption; set { _selectedStatusOption = value; OnPropertyChanged(nameof(SelectedStatusOption)); } }
    public string NewGenreName { get => _newGenreName; set { _newGenreName = value; OnPropertyChanged(nameof(NewGenreName)); } }
    public string Description { get => _description; set { _description = value; OnPropertyChanged(nameof(Description)); } }
    public ImageSource? PosterPreview { get => _posterPreview; private set { _posterPreview = value; OnPropertyChanged(nameof(PosterPreview)); } }
    public string PosterFileName { get => _posterFileName; private set { _posterFileName = value; OnPropertyChanged(nameof(PosterFileName)); } }
    public bool HasPoster => !string.IsNullOrWhiteSpace(_pendingPosterSourcePath) || !string.IsNullOrWhiteSpace(_posterPath);

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set { _errorMessage = value; OnPropertyChanged(nameof(ErrorMessage)); OnPropertyChanged(nameof(HasError)); }
    }
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public string? SuccessMessage
    {
        get => _successMessage;
        private set { _successMessage = value; OnPropertyChanged(nameof(SuccessMessage)); OnPropertyChanged(nameof(HasSuccess)); }
    }
    public bool HasSuccess => !string.IsNullOrWhiteSpace(SuccessMessage);

    public override void LoadData()
    {
        int? selectedId = _editingMovieId;
        RefreshGenres();

        _allMovies.Clear();
        _allMovies.AddRange(MovieData.LoadAllMovies());
        ApplyFilter();

        if (selectedId.HasValue && _allMovies.FirstOrDefault(m => m.MovieId == selectedId.Value) is { } matched)
        {
            SelectedMovie = matched;
            return;
        }

        if (!IsEditMode)
        {
            ResetFormFields();
        }
    }

    public void ResetFilters()
    {
        _searchKeyword = string.Empty;
        _selectedGenreFilter = GenreFilterOptions.FirstOrDefault();
        _selectedAgeRatingFilter = "Tất cả";
        _selectedStatusFilterOption = StatusFilterOptions[0];

        OnPropertyChanged(nameof(SearchKeyword));
        OnPropertyChanged(nameof(SelectedGenreFilter));
        OnPropertyChanged(nameof(SelectedAgeRatingFilter));
        OnPropertyChanged(nameof(SelectedStatusFilterOption));
        ApplyFilter();
    }

    public void StartNewMovie()
    {
        ClearMessages();
        SelectedMovie = null;
        ResetFormFields();
    }

    public void ResetForm() => StartNewMovie();

    public void SetPosterFromFile(string sourcePath)
    {
        ClearMessages();
        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
        {
            ErrorMessage = "Không tìm thấy file ảnh đã chọn.";
            return;
        }

        try
        {
            PosterPreview = CinemaHelper.LoadPosterBitmap(sourcePath);
            _pendingPosterSourcePath = sourcePath;
            PosterFileName = Path.GetFileName(sourcePath);
            OnPropertyChanged(nameof(HasPoster));
        }
        catch (Exception)
        {
            ErrorMessage = "Không thể đọc file ảnh đã chọn. Hãy thử ảnh JPG hoặc PNG hợp lệ.";
        }
    }

    public void ClearPoster()
    {
        _pendingPosterSourcePath = null;
        _posterPath = null;
        PosterPreview = null;
        PosterFileName = "Chưa chọn ảnh (JPG, PNG)";
        OnPropertyChanged(nameof(HasPoster));
    }

    public void AddGenre()
    {
        ClearMessages();
        string name = NewGenreName.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            ErrorMessage = "Vui lòng nhập tên thể loại mới.";
            return;
        }

        GenreSelectionItem? existing = GenreOptions.FirstOrDefault(g =>
            string.Equals(g.Name, name, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            existing.IsSelected = true;
            NewGenreName = string.Empty;
            SuccessMessage = $"Đã chọn thể loại \"{existing.Name}\".";
            return;
        }

        int genreId = MovieData.InsertGenre(name);
        GenreOptions.Add(new GenreSelectionItem(genreId, name) { IsSelected = true });
        GenreFilterOptions.Add(new GenreFilterOption(genreId, name));
        NewGenreName = string.Empty;
        SuccessMessage = $"Đã thêm thể loại \"{name}\".";
    }

    public void SaveMovie()
    {
        ClearMessages();
        if (!TryBuildMovieInput(out MovieSaveInput? input))
        {
            return;
        }

        bool wasEdit = _editingMovieId.HasValue;
        int savedId = MovieData.SaveMovie(_editingMovieId, input!);

        _editingMovieId = savedId;
        _pendingPosterSourcePath = null;
        _posterPath = input!.PosterPath;
        NotifyModeChanged();

        LoadData();
        SuccessMessage = wasEdit
            ? $"Đã cập nhật phim \"{input.Title}\"."
            : $"Đã thêm phim mới \"{input.Title}\".";
    }

    public bool CanDeleteSelectedMovie(out string movieTitle)
    {
        ClearMessages();
        movieTitle = Title.Trim();
        if (!_editingMovieId.HasValue)
        {
            ErrorMessage = "Vui lòng chọn một phim trong danh sách để xóa.";
            return false;
        }

        int showtimeCount = MovieData.CountMovieShowtimes(_editingMovieId.Value);
        if (showtimeCount > 0)
        {
            ErrorMessage = $"Phim này đang có {showtimeCount} suất chiếu nên không thể xóa. Hãy đổi trạng thái sang Ngừng chiếu (Ended).";
            return false;
        }

        return true;
    }

    public void DeleteSelectedMovie()
    {
        if (!_editingMovieId.HasValue)
        {
            return;
        }

        string deletedTitle = Title.Trim();
        MovieData.DeleteMovie(_editingMovieId.Value);

        SelectedMovie = null;
        ResetFormFields();
        LoadData();
        SuccessMessage = $"Đã xóa phim \"{deletedTitle}\".";
    }

    private void RefreshGenres()
    {
        HashSet<int> checkedIds = GenreOptions.Where(g => g.IsSelected).Select(g => g.GenreId).ToHashSet();
        int? activeFilterId = _selectedGenreFilter?.GenreId;

        GenreOptions.Clear();
        GenreFilterOptions.Clear();
        GenreFilterOptions.Add(new GenreFilterOption(null, "Tất cả"));

        foreach ((int id, string name) in MovieData.LoadAllGenres())
        {
            GenreOptions.Add(new GenreSelectionItem(id, name) { IsSelected = checkedIds.Contains(id) });
            GenreFilterOptions.Add(new GenreFilterOption(id, name));
        }

        _selectedGenreFilter = activeFilterId.HasValue
            ? GenreFilterOptions.FirstOrDefault(g => g.GenreId == activeFilterId.Value) ?? GenreFilterOptions[0]
            : GenreFilterOptions[0];
        OnPropertyChanged(nameof(SelectedGenreFilter));
    }

    private void ApplyFilter()
    {
        Movies.Clear();
        string keyword = SearchKeyword.Trim();
        int? genreId = SelectedGenreFilter?.GenreId;
        string age = SelectedAgeRatingFilter;
        string status = SelectedStatusFilterOption?.Value ?? "All";

        foreach (AdminMovieRow m in _allMovies)
        {
            if (status != "All" && m.Status != status) continue;
            if (genreId.HasValue && !m.GenreIds.Contains(genreId.Value)) continue;
            if (age != "Tất cả" && m.AgeRatingText != age) continue;

            if (!string.IsNullOrEmpty(keyword)
                && !ContainsKeyword(m.Title, keyword)
                && !ContainsKeyword(m.OriginalTitle, keyword)
                && !ContainsKeyword(m.Director, keyword)
                && !ContainsKeyword(m.Cast, keyword))
            {
                continue;
            }

            Movies.Add(m);
        }

        ResultCountText = Movies.Count == _allMovies.Count
            ? $"{Movies.Count} phim"
            : $"{Movies.Count} / {_allMovies.Count} phim";
    }

    private void PopulateForm(AdminMovieRow movie)
    {
        ClearMessages();
        _editingMovieId = movie.MovieId;
        _pendingPosterSourcePath = null;
        _posterPath = movie.PosterPath;

        Title = movie.Title;
        OriginalTitle = movie.OriginalTitle ?? string.Empty;
        Director = movie.Director ?? string.Empty;
        Cast = movie.Cast ?? string.Empty;
        DurationText = movie.DurationMinutes.ToString(CultureInfo.InvariantCulture);
        Language = movie.Language;
        ReleaseDateText = FormatDisplayDate(movie.ReleaseDateSql);
        EndDateText = string.IsNullOrWhiteSpace(movie.EndDateSql) ? string.Empty : FormatDisplayDate(movie.EndDateSql);
        SelectedAgeRating = AgeRatingOptions.Contains(movie.AgeRatingText) ? movie.AgeRatingText : "P";
        SelectedStatusOption = StatusOptions.FirstOrDefault(s => s.Value == movie.Status) ?? StatusOptions[0];
        Description = movie.Description ?? string.Empty;

        HashSet<int> selectedSet = [.. movie.GenreIds];
        foreach (GenreSelectionItem genre in GenreOptions)
        {
            genre.IsSelected = selectedSet.Contains(genre.GenreId);
        }

        LoadPosterFromStoredPath(movie.PosterPath);
        OnPropertyChanged(nameof(HasPoster));
        NotifyModeChanged();
    }

    private void ResetFormFields()
    {
        _editingMovieId = null;
        Title = string.Empty;
        OriginalTitle = string.Empty;
        Director = string.Empty;
        Cast = string.Empty;
        DurationText = string.Empty;
        Language = "Phụ đề Tiếng Việt";
        ReleaseDateText = DateTime.Today.ToString("dd/MM/yyyy");
        EndDateText = string.Empty;
        SelectedAgeRating = "P";
        SelectedStatusOption = StatusOptions[0];
        NewGenreName = string.Empty;
        Description = string.Empty;

        foreach (GenreSelectionItem genre in GenreOptions)
        {
            genre.IsSelected = false;
        }

        ClearPoster();
        NotifyModeChanged();
    }

    private bool TryBuildMovieInput(out MovieSaveInput? input)
    {
        input = null;
        List<int> genreIds = GenreOptions.Where(g => g.IsSelected).Select(g => g.GenreId).ToList();

        if (string.IsNullOrWhiteSpace(Title)) { ErrorMessage = "Tên phim không được để trống."; return false; }
        if (!int.TryParse(DurationText.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int duration) || duration <= 0)
        { ErrorMessage = "Thời lượng phim phải là số nguyên lớn hơn 0 (phút)."; return false; }
        if (string.IsNullOrWhiteSpace(Language)) { ErrorMessage = "Ngôn ngữ phim không được để trống."; return false; }
        if (!TryParseDate(ReleaseDateText, out DateOnly releaseDate))
        { ErrorMessage = "Ngày khởi chiếu không hợp lệ (hãy nhập dạng dd/MM/yyyy hoặc yyyy-MM-dd)."; return false; }

        string? endSql = null;
        if (!string.IsNullOrWhiteSpace(EndDateText))
        {
            if (!TryParseDate(EndDateText, out DateOnly endDate))
            { ErrorMessage = "Ngày kết thúc không hợp lệ (hãy nhập dạng dd/MM/yyyy hoặc yyyy-MM-dd)."; return false; }
            if (endDate < releaseDate)
            { ErrorMessage = "Ngày kết thúc không được nhỏ hơn ngày khởi chiếu."; return false; }
            endSql = endDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        if (genreIds.Count == 0) { ErrorMessage = "Vui lòng chọn ít nhất 1 thể loại cho phim."; return false; }

        string? posterPath = !string.IsNullOrWhiteSpace(_pendingPosterSourcePath) && File.Exists(_pendingPosterSourcePath)
            ? CinemaHelper.SavePosterFile(_pendingPosterSourcePath)
            : _posterPath;

        input = new MovieSaveInput(
            Title.Trim(),
            TrimOrNull(OriginalTitle),
            TrimOrNull(Director),
            TrimOrNull(Cast),
            duration,
            releaseDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            endSql,
            SelectedAgeRating,
            Language.Trim(),
            TrimOrNull(Description),
            posterPath,
            SelectedStatusOption?.Value ?? "NowShowing",
            genreIds);
        return true;
    }

    private void LoadPosterFromStoredPath(string? storedPath)
    {
        if (string.IsNullOrWhiteSpace(storedPath))
        {
            PosterPreview = null;
            PosterFileName = "Chưa chọn ảnh (JPG, PNG)";
            return;
        }

        string fullPath = CinemaHelper.ResolvePosterFullPath(storedPath);
        if (!File.Exists(fullPath))
        {
            PosterPreview = null;
            PosterFileName = $"Không tìm thấy file ({Path.GetFileName(storedPath)})";
            return;
        }

        PosterPreview = CinemaHelper.LoadPosterBitmap(fullPath);
        PosterFileName = Path.GetFileName(storedPath);
    }

    private void NotifyModeChanged()
    {
        OnPropertyChanged(nameof(IsEditMode));
        OnPropertyChanged(nameof(FormHeaderText));
        OnPropertyChanged(nameof(FormModeBadge));
    }

    private void ClearMessages() { ErrorMessage = null; SuccessMessage = null; }

    private static bool ContainsKeyword(string? field, string keyword)
        => !string.IsNullOrEmpty(field) && field.Contains(keyword, StringComparison.OrdinalIgnoreCase);

    private static bool TryParseDate(string input, out DateOnly date)
        => DateOnly.TryParseExact(input.Trim(), DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

    private static string FormatDisplayDate(string sqlDate)
        => DateOnly.TryParseExact(sqlDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly d)
            ? d.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)
            : sqlDate;

    private static string? TrimOrNull(string value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
