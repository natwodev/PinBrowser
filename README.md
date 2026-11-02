# Pin Browser

Cửa sổ trình duyệt nhỏ gọn có thể **ghim lên màn hình**, đặt ở vị trí bất kỳ, thu nhỏ/phóng to tùy ý và chỉnh **độ trong suốt** để chống nhìn trộm. Dùng engine Microsoft Edge (WebView2).

## Chức năng

- **Ghim** (📌, Ctrl+T): cửa sổ luôn nằm trên mọi cửa sổ khác.
- **Độ trong suốt**: thanh trượt 20% đến 100%.
- **Trình duyệt**: quay lại, tiến tới, tải lại (F5), thanh địa chỉ (Ctrl+L) nhận URL hoặc từ khóa tìm Google. Link `target=_blank` mở ngay trong cùng cửa sổ.
- **Nhớ trạng thái**: trang đang xem, vị trí, kích thước, độ trong suốt, trạng thái ghim. Lưu ở `%AppData%\PinBrowser\settings.json`.
- Profile trình duyệt (cookie, đăng nhập) lưu ở `%LocalAppData%\PinBrowser\WebView2`.

## Yêu cầu

Windows x64 và Microsoft Edge WebView2 Runtime (Windows 11 thường có sẵn). Không cần cài .NET.

## Build

```
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o publish
```

Kết quả là một file `publish\PinBrowser.exe` chạy được trên Windows x64 mà không cần cài .NET.

## Cấu trúc

| Đường dẫn | Nội dung |
|---|---|
| `Program.cs` | Điểm vào |
| `MainForm.cs` | Cửa sổ chính: thanh công cụ, ghim, độ trong suốt, WebView2 |
| `Settings.cs` | Đọc/ghi trạng thái |
| `Assets/` | Icon của app |
