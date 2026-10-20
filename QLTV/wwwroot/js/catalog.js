// Tra cứu sách tức thì: gọi GET /api/books bằng AJAX, không tải lại trang.
// Nếu JavaScript bị tắt thì form vẫn hoạt động bình thường nhờ trang do server dựng sẵn.
(function ($) {
    'use strict';

    var $form = $('#catalogForm');
    if ($form.length === 0) return;

    var $grid = $('#bookGrid');
    var $count = $('#resultCount');
    var $pager = $('#serverPager');
    var $more = $('#loadMore');
    var $empty = $('#emptyMsg');
    var state = { page: 1, totalPages: 1, seq: 0 };
    var timer = null;

    // Mọi dữ liệu chèn vào HTML đều được mã hóa để chống XSS (tên sách do admin nhập)
    function esc(value) {
        return String(value == null ? '' : value)
            .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;').replace(/'/g, '&#39;');
    }

    function card(b) {
        var cover = b.coverImagePath
            ? '<img src="' + esc(b.coverImagePath) + '" alt="Bìa sách ' + esc(b.title) + '" loading="lazy" />'
            : '<div class="cover-placeholder" style="background:hsl(' + ((b.id * 47) % 360) + ',50%,48%)">' +
              esc((b.title || '?').charAt(0).toUpperCase()) + '</div>';

        var badge = b.borrowable > 0
            ? '<span class="badge text-bg-success">Còn ' + b.borrowable + ' bản</span>'
            : (b.readOnSite > 0
                ? '<span class="badge text-bg-info">Chỉ đọc tại chỗ</span>'
                : '<span class="badge text-bg-secondary">Tạm hết</span>');

        return '<div class="col"><div class="card book-card h-100">' +
            '<a href="' + esc(b.url) + '" class="book-cover" aria-label="' + esc(b.title) + '">' + cover + '</a>' +
            '<div class="card-body pb-2">' +
            '<div class="small text-muted">' + esc(b.category) + '</div>' +
            '<h3 class="card-title h6 mb-1"><a href="' + esc(b.url) + '">' + esc(b.title) + '</a></h3>' +
            '<div class="small text-muted">' + esc(b.author) + '</div></div>' +
            '<div class="card-footer bg-transparent border-0 pt-0 pb-3">' + badge + '</div>' +
            '</div></div>';
    }

    function filters() {
        var p = { sort: $form.find('[name=sort]').val() };
        var q = $.trim($form.find('[name=q]').val());
        var cat = $form.find('[name=categoryId]').val();
        if (q) p.q = q;
        if (cat) p.categoryId = cat;
        if ($form.find('[name=availableOnly]').is(':checked')) p.availableOnly = true;
        return p;
    }

    function load(page, append) {
        var seq = ++state.seq;   // bỏ qua kết quả của những lần gọi cũ trả về chậm
        var params = $.extend({ page: page, pageSize: 12 }, filters());
        $grid.addClass('opacity-50');

        $.getJSON('/api/books', params)
            .done(function (data) {
                if (seq !== state.seq) return;
                var html = $.map(data.items, card).join('');
                if (append) { $grid.append(html); } else { $grid.html(html); }

                state.page = data.page;
                state.totalPages = data.totalPages;
                $count.text(data.totalItems.toLocaleString('vi-VN'));
                $empty.toggleClass('d-none', data.items.length > 0 || append);
                $pager.addClass('d-none');                                  // thay phân trang của server bằng "Xem thêm"
                $more.toggleClass('d-none', data.page >= data.totalPages);

                if (!append && window.history && history.replaceState) {    // giữ địa chỉ có thể chia sẻ
                    var qs = $.param(filters());
                    history.replaceState(null, '', location.pathname + (qs ? '?' + qs : ''));
                }
            })
            .fail(function () {
                if (seq === state.seq) {
                    $grid.html('<div class="col-12 text-danger">Không tải được dữ liệu. Vui lòng thử lại.</div>');
                }
            })
            .always(function () {
                if (seq === state.seq) $grid.removeClass('opacity-50');
            });
    }

    $form.on('submit', function (e) { e.preventDefault(); load(1, false); });

    // Gõ tới đâu tìm tới đó (chờ 300ms sau lần gõ cuối để không gọi API liên tục)
    $form.find('[name=q]').on('input', function () {
        clearTimeout(timer);
        timer = setTimeout(function () { load(1, false); }, 300);
    });
    $form.find('[name=categoryId], [name=sort], [name=availableOnly]').on('change', function () { load(1, false); });

    $more.on('click', function () { load(state.page + 1, true); });
})(jQuery);
