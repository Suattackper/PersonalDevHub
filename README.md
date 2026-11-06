# DevHub

**DevHub** là bộ công cụ dành cho lập trình viên, chạy trực tiếp trong trình duyệt. Đây là một dự án trong portfolio của **Nguyễn Trần Anh Tân**, Software Engineer, sinh năm **2003**. Commit đầu tiên của dự án được tạo ngày **07/08/2026**. Trang `/about` giới thiệu tác giả và các công nghệ được sử dụng.

**Dùng thử:** [suattackper.github.io/PersonalDevHub](https://suattackper.github.io/PersonalDevHub/)

Ứng dụng được viết bằng **.NET 8 Blazor WebAssembly** và **C# 12**, với giao diện **MudBlazor 8**. Ứng dụng không cần máy chủ API, cơ sở dữ liệu hay tài khoản đăng nhập. Dữ liệu được lưu trong `localStorage` của trình duyệt với tiền tố `devhub.`. Bản phát hành là PWA có thể cài đặt và dùng ngoại tuyến sau khi tài nguyên đã được lưu vào bộ nhớ đệm.

Tiếng Việt là ngôn ngữ mặc định; người dùng có thể chuyển sang tiếng Anh trong Cài đặt. Một số thông báo động vẫn cần rà soát; xem [kế hoạch đa ngôn ngữ](docs/image-and-localization-plan.md).

## Chức năng

- **Biên tập và so sánh:** định dạng và kiểm tra JSON; viết Markdown với xem trước và xuất HTML; so sánh văn bản theo dòng; định dạng SQL; mã hóa và giải mã HTML.
- **Mã hóa và phân tích:** Base64 cho văn bản và tệp; mã hóa URL và phân tích URL; đọc JWT; tạo và kiểm tra mã băm MD5, SHA-1, SHA-256, SHA-384, SHA-512.
- **Tạo dữ liệu:** UUID v4, mật khẩu, văn bản mẫu, mã QR và mã vạch.
- **Chuyển đổi:** kiểu chữ, hệ cơ số, thời gian, JSON/YAML/XML và màu sắc.
- **Tra cứu và kiểm tra:** Regex, loại MIME, mã trạng thái HTTP và biểu thức cron.
- **Công cụ ảnh:** chọn tệp, kéo thả hoặc dán ảnh bằng `Ctrl+V`; lấy mã màu từ điểm ảnh; xóa vùng nền **liền kề có màu gần đồng nhất** từ điểm được chọn; so sánh trước/sau, khôi phục và tải PNG trong suốt. Công cụ chưa xử lý tốt nền ảnh chụp phức tạp.
- **Trải nghiệm ứng dụng:** Dashboard, tìm kiếm, yêu thích, ghim, lịch sử, hướng dẫn sử dụng theo công cụ, cài đặt giao diện và ngôn ngữ.

### Một số lưu ý kỹ thuật

- `JsonFormatter` dùng `Utf8JsonWriter` để giữ đúng escape, Unicode và độ chính xác số. Monaco được đóng gói cùng ứng dụng để dùng ngoại tuyến. Tài liệu JSON lớn hơn 512 KB tạm ngừng quét khóa trùng và dựng cây; trên 256 KB không tự lưu nội dung vào workspace để tránh vượt hạn mức `localStorage`.
- Bản xem trước Markdown tắt HTML thô để nội dung được dán vào không chạy mã tùy ý. `DiffEngine` chuyển sang so sánh tuyến tính khi văn bản vượt 4.000 dòng và đánh dấu kết quả là xấp xỉ.
- `JwtDecoder` **chỉ đọc token, không xác minh chữ ký**. MD5 được triển khai trong `Md5Digest` vì WebAssembly không hỗ trợ `System.Security.Cryptography.MD5`; MD5 và SHA-1 chỉ dành cho checksum hoặc tương thích hệ thống cũ.
- `PasswordGenerator` dùng `RandomNumberGenerator`. Mật khẩu đã tạo không được lưu vào workspace hoặc lịch sử. `RegexTester` giới hạn mỗi lần chạy ở 2 giây để tránh biểu thức backtracking làm treo tab.
- QR dùng renderer managed của QRCoder để chạy trong WebAssembly. `BarcodeEncoder` hỗ trợ Code 128, EAN-13, EAN-8, UPC-A và ITF. Hãy kiểm tra bằng máy quét thật trước khi in nhãn sản xuất.
- `ColorConverter` đọc HEX, RGB, RGBA, HSL và chuyển đổi giữa HEX, RGB, HSL, HSV, CMYK. Độ tương phản được tính theo WCAG 2.
- `CronParser` hỗ trợ cron Linux 5 trường và Quartz có giây, tùy chọn năm. Việc tìm lần chạy kế tiếp giới hạn trong 5 năm để tránh lặp vô hạn.

### Hiệu năng JSON

Kết quả đo trên máy phát triển với bản Release và dữ liệu thử gồm 20.000 đối tượng:

| Kích thước tài liệu | Thời gian định dạng |
|---|---:|
| 30 KB | khoảng 0,19 giây |
| 185 KB | khoảng 0,85 giây |
| 1,27 MB, xuất ra 2,6 MB và 180.000 dòng | khoảng 6,2 giây; riêng `setValue` của Monaco chiếm khoảng 2,4 giây |

Cây JSON chỉ tải nút con khi người dùng mở nhánh và giới hạn 100 mục mỗi container. `JsonFormatter.Inspect` gộp kiểm tra hợp lệ, khóa trùng và dựng dữ liệu cây trong một lần phân tích. Ứng dụng cũng chặn sự kiện thay đổi lặp lại khi đẩy dữ liệu vào Monaco.

### Chi tiết các công cụ khác

- `Base64Converter` nhận data URI, bảng chữ cái URL-safe, thiếu padding và xuống dòng. Loại tệp được nhận diện từ byte đặc trưng thay vì chỉ tin MIME được khai báo.
- `UrlToolkit` phân biệt mã hóa một thành phần URL với mã hóa toàn bộ URL. Danh sách query có thể sửa và mọi thay đổi sẽ cập nhật lại URL.
- `DataConversionService` dùng `JsonNode` làm biểu diễn trung gian cho JSON, YAML và XML. Thuộc tính XML thành khóa `@name`; phần tử lặp thành mảng.
- `UuidGenerator` tạo UUID v4 hàng loạt, hỗ trợ năm cách hiển thị và xuất TXT/CSV/JSON. `LoremGenerator` có seed để tái tạo kết quả và luôn dùng `\\n` khi xuống dòng.
- `CaseConverter` dùng chung một bộ tách từ để giữ đúng các cụm viết tắt. `NumberBaseConverter` nhận tiền tố `0x`, `0b`, `0o`, dấu và ký tự phân cách, đồng thời đọc văn bản theo Unicode code point.
- `TimestampConverter` đọc Unix giây/mili giây, ISO 8601 và các định dạng phổ biến. Giá trị thiếu múi giờ được coi là UTC; ứng dụng có dữ liệu của 313 múi giờ.
- `MimeCatalog` và `HttpStatusCatalog` là dữ liệu tra cứu. Tìm kiếm ưu tiên kết quả khớp chính xác; kiểm thử bảo đảm các mã HTTP liên quan đều tồn tại.
- Khi cả hai trường ngày của cron bị giới hạn, lịch dùng hợp của chúng: `0 9 1 * MON` chạy vào ngày 1 và mọi thứ Hai. Lịch không thể xảy ra, ví dụ ngày 30 tháng 2, được báo lỗi sau khi tìm trong giới hạn năm năm.

## Chạy trên máy

Yêu cầu .NET 8 SDK.

```bash
dotnet run --project DevhubWebApp/DevhubWebApp/DevhubWebApp.csproj
```

Chạy kiểm thử:

```bash
dotnet test DevhubWebApp/DevhubWebApp.sln
```

Tạo bản phát hành có service worker và bộ nhớ đệm ngoại tuyến:

```bash
dotnet publish DevhubWebApp/DevhubWebApp/DevhubWebApp.csproj -c Release
```

Khi đo hiệu năng, hãy dùng bản **Release** vì Blazor WebAssembly ở chế độ Debug chậm hơn đáng kể.

## Triển khai

[Workflow GitHub Pages](.github/workflows/deploy-pages.yml) tự build và phát hành khi có commit mới trên `main`. Repository cần chọn **GitHub Actions** làm nguồn phát hành tại Settings → Pages. Workflow đặt base path `/PersonalDevHub/` trước khi publish và tạo `404.html` để mở trực tiếp các đường dẫn như `/about` hoặc `/tools/json`.

## Cấu trúc và kiến trúc

```text
DevhubWebApp/
└── DevhubWebApp/
    ├── Components/     Thành phần giao diện, điều hướng và các công cụ
    ├── Constants/      Hằng số, route, danh mục công cụ và nội dung dịch
    ├── Extensions/     Đăng ký DI và các hàm mở rộng
    ├── Interfaces/     Hợp đồng của các dịch vụ
    ├── JSInterop/      Lớp gọi API trình duyệt
    ├── Layout/         Khung giao diện chính
    ├── Models/         Mô hình dữ liệu và cài đặt
    ├── Pages/          Các trang có route
    ├── Services/       Logic ứng dụng, tùy chọn và lịch sử
    ├── Storage/        Triển khai IStorageService
    ├── Themes/         Màu sắc và kiểu chữ MudBlazor
    ├── Utilities/      Thuật toán độc lập với giao diện
    └── wwwroot/js/     Mô-đun JavaScript
```

`Pages` điều phối trang; `Components` tạo giao diện có thể tái sử dụng; `Services` quản lý trạng thái và nghiệp vụ; `Utilities` chứa thuật toán độc lập; `JSInterop` đóng gói API trình duyệt. Các thành phần phụ thuộc vào interface được đăng ký trong `Extensions/ServiceCollectionExtensions.cs`.

```text
Pages / Components → Interfaces → Services → IStorageService → JS module → localStorage
                                   └────────→ IBrowserInteropService
```

`UserPreferenceService` quản lý cài đặt, yêu thích, ghim, công cụ gần đây và thống kê sử dụng. `HistoryService` quản lý lịch sử thao tác. Trạng thái được tải trong `App.razor` trước khi router hiển thị. Khóa lưu trữ, mã công cụ và route được tập trung trong `Constants/`.

Mỗi công cụ là một Razor component có `[Tool]`. `AttributeToolDiscovery` tìm công cụ lúc khởi động; `ToolRegistry` kiểm tra và sắp xếp; `IToolCatalog` kết hợp metadata với tùy chọn người dùng. Route `/tools/{ToolId}` dùng `DynamicComponent` để hiển thị công cụ. `ToolComponentBase<TWorkspace>` cung cấp workspace, undo/redo, debounce, phím tắt và ghi lịch sử.

### Thêm công cụ mới

1. Thêm ID vào `Constants/ToolIds.cs`.
2. Tạo `Components/Tools/MyTool.razor` với `[Tool(...)]` và kế thừa `ToolComponentBase<TWorkspace>`.
3. Đặt thuật toán trong `Utilities/` hoặc dịch vụ có trạng thái trong `Services/`; viết kiểm thử cho thuật toán.
4. Ghi thao tác cần thiết bằng `RecordAsync(...)`.

Sidebar, tìm kiếm, Dashboard, yêu thích, ghim và lịch sử sẽ tự nhận công cụ mới từ danh mục.

### Dashboard và thành phần giao diện

`Pages/Dashboard.razor` ghép các phần hiển thị. Mỗi phần trong `Components/Dashboard/` lấy dữ liệu từ `IDashboardService`, có trạng thái tải, trống, lỗi và thử lại riêng. Các khu vực Ghim, Yêu thích, Gần đây và Dùng nhiều cùng sử dụng `ToolSection` với nguồn dữ liệu khác nhau.

Các thành phần giao diện dùng chung nằm trong `Components/Shared`; trang `/playground` hiển thị chúng để kiểm tra trực quan.

| Thành phần | Vai trò |
|---|---|
| `AppCard`, `ToolCard`, `StatCard` | Khung nội dung, thẻ công cụ và chỉ số |
| `EmptyState`, `ErrorState`, `StateMessage` | Trạng thái trống và lỗi |
| `LoadingSkeleton`, `LoadingOverlay` | Trạng thái đang tải |
| `PageHeader`, `SectionTitle`, `DividerTitle` | Tiêu đề trang và khu vực |
| `Toolbar`, `SearchBox` | Thanh thao tác và tìm kiếm |
| `CopyButton`, `DownloadButton`, `IconButton` | Sao chép, tải tệp và thao tác biểu tượng |
| `FileDropZone`, `ConfirmDialog` | Nhận tệp và xác nhận thao tác |
| `Badge`, `Chip`, `SettingItem` | Nhãn, thẻ tương tác và mục cài đặt |

## Quy ước đóng góp

1. Tạo nhánh từ `main` và thực hiện thay đổi có phạm vi rõ ràng.
2. Dùng namespace dạng file-scoped, ngoặc nhọn cho khối lệnh và hậu tố `Async` cho phương thức bất đồng bộ; không dùng `async void`.
3. Giữ logic có thể kiểm thử ngoài Razor khi phù hợp. Thành phần có code-behind sử dụng cặp `.razor` và `.razor.cs`.
4. Chạy `dotnet build` và kiểm thử trước khi commit. Cảnh báo build được coi là lỗi theo cấu hình dự án.
5. Viết commit message ở thể mệnh lệnh, có tiền tố phạm vi, ví dụ `tools(json): add minify action`.

## Tiến độ

| Nhóm | Trạng thái |
|---|---|
| Nền tảng PWA, giao diện chung, khám phá công cụ và Dashboard | Đã triển khai |
| JSON, Markdown, so sánh văn bản, Base64, URL, JWT, Hash | Đã triển khai |
| UUID, Regex, mật khẩu, văn bản mẫu, kiểu chữ, hệ cơ số | Đã triển khai |
| QR, mã vạch (5 chuẩn), màu sắc, thời gian, MIME, HTTP status, Cron | Đã triển khai |
| Chuyển đổi JSON/YAML/XML, định dạng SQL, mã hóa HTML | Đã triển khai |
| Settings, History, About và công cụ ảnh | Đã triển khai |

Danh sách trên phản ánh các mô-đun đã triển khai. Phần đa ngôn ngữ và trải nghiệm chi tiết ở từng công cụ vẫn tiếp tục được rà soát.
