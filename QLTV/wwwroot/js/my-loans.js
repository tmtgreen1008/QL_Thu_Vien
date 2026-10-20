// Gia hạn sách không tải lại trang: POST /api/me/loans/{id}/renew kèm token chống CSRF.
// Nếu JavaScript bị tắt thì form vẫn gửi lên action Renew của server.
(function ($) {
    'use strict';

    var token = $('#ajaxToken input[name="__RequestVerificationToken"]').val();

    function showAlert(type, text) {
        var $alert = $('<div class="alert alert-dismissible fade show" role="alert"></div>')
            .addClass('alert-' + type)
            .text(text)   // .text() tự mã hóa nội dung, không chèn HTML
            .append('<button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Đóng"></button>');
        $('#loanAlert').empty().append($alert);
    }

    function formatDate(iso) {            // "2026-10-26" -> "26/10/2026"
        var p = iso.split('-');
        return p[2] + '/' + p[1] + '/' + p[0];
    }

    function daysLeft(iso) {
        var due = new Date(iso + 'T00:00:00');
        var today = new Date();
        today.setHours(0, 0, 0, 0);
        return Math.round((due - today) / 86400000);
    }

    $(document).on('submit', '.renew-form', function (e) {
        e.preventDefault();

        var $form = $(this);
        var $button = $form.find('button');
        var $row = $form.closest('tr');
        var id = $form.data('item-id');

        $button.prop('disabled', true);

        $.ajax({
            url: '/api/me/loans/' + encodeURIComponent(id) + '/renew',
            method: 'POST',
            headers: { 'RequestVerificationToken': token },
            dataType: 'json'
        })
        .done(function (r) {
            $row.find('.due-date').text(formatDate(r.dueDate));
            $row.find('.due-hint').text('còn ' + daysLeft(r.dueDate) + ' ngày');
            $row.find('.renew-count').text(r.renewCount);

            if (r.renewCount >= r.maxRenewals) {
                $form.replaceWith('<span class="small text-muted">Hết lượt gia hạn</span>');
            } else {
                $button.prop('disabled', false);
            }
            showAlert('success', r.message);
        })
        .fail(function (xhr) {
            var msg = 'Không gia hạn được. Vui lòng thử lại.';
            if (xhr.status === 401) {
                msg = 'Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.';
            } else if (xhr.responseJSON && (xhr.responseJSON.detail || xhr.responseJSON.title)) {
                msg = xhr.responseJSON.detail || xhr.responseJSON.title;
            }
            showAlert('danger', msg);
            $button.prop('disabled', false);
        });
    });
})(jQuery);
