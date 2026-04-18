document.addEventListener('DOMContentLoaded', function () {
    setupLikeInteraction();
    setupBookmarkInteraction();
});

function setupLikeInteraction() {
    const likeForm = document.getElementById('like-form');
    if (!likeForm) return;

    likeForm.addEventListener('submit', async function (e) {
        e.preventDefault();

        const slug = likeForm.dataset.slug;
        const btn = document.getElementById('like-btn');
        const icon = document.getElementById('like-icon');
        const countSpan = document.getElementById('like-count');
        const token = likeForm.querySelector('input[name="__RequestVerificationToken"]').value;

        try {
            // Disable button temporarily
            btn.disabled = true;

            const response = await fetch(`/post/${slug}?handler=LikeJson`, {
                method: 'POST',
                headers: {
                    'X-CSRF-TOKEN': token,
                    'X-Requested-With': 'XMLHttpRequest'
                }
            });

            if (response.ok) {
                const result = await response.json();

                if (result.success) {
                    // Update UI
                    if (result.isLiked) {
                        btn.classList.add('active');
                        icon.classList.remove('far');
                        icon.classList.add('fas');
                    } else {
                        btn.classList.remove('active');
                        icon.classList.remove('fas');
                        icon.classList.add('far');
                    }
                    countSpan.textContent = result.count;
                } else if (result.redirectUrl) {
                    window.location.href = result.redirectUrl;
                } else if (result.error) {
                    console.error('Error:', result.error);
                }
            } else {
                console.error('Network response was not ok');
            }
        } catch (error) {
            console.error('Error:', error);
        } finally {
            btn.disabled = false;
        }
    });
}

function setupBookmarkInteraction() {
    const bookmarkForm = document.getElementById('bookmark-form');
    if (!bookmarkForm) return;

    bookmarkForm.addEventListener('submit', async function (e) {
        e.preventDefault();

        const slug = bookmarkForm.dataset.slug;
        const btn = document.getElementById('bookmark-btn');
        const icon = document.getElementById('bookmark-icon');
        const token = bookmarkForm.querySelector('input[name="__RequestVerificationToken"]').value;

        try {
            btn.disabled = true;

            const response = await fetch(`/post/${slug}?handler=BookmarkJson`, {
                method: 'POST',
                headers: {
                    'X-CSRF-TOKEN': token,
                    'X-Requested-With': 'XMLHttpRequest'
                }
            });

            if (response.ok) {
                const result = await response.json();

                if (result.success) {
                    if (result.isBookmarked) {
                        btn.classList.add('active');
                        icon.classList.remove('far');
                        icon.classList.add('fas');
                    } else {
                        btn.classList.remove('active');
                        icon.classList.remove('fas');
                        icon.classList.add('far');
                    }
                } else if (result.redirectUrl) {
                    window.location.href = result.redirectUrl;
                }
            }
        } catch (error) {
            console.error('Error:', error);
        } finally {
            btn.disabled = false;
        }
    });
}
