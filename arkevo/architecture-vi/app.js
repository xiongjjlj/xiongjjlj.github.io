(() => {
  const photos = JSON.parse(document.getElementById('photo-data').textContent);
  const cards = [...document.querySelectorAll('.architect')];
  const filters = [...document.querySelectorAll('[data-filter]')];
  const count = document.getElementById('count');
  function filter(group) {
    let shown = 0;
    cards.forEach(card => { card.hidden = group !== 'all' && card.dataset.group !== group; if (!card.hidden) shown++; });
    filters.forEach(button => { const active = button.dataset.filter === group; button.classList.toggle('active', active); button.setAttribute('aria-pressed', String(active)); });
    count.textContent = group === 'all' ? `显示全部 ${shown} 位` : `当前方向 · ${shown} 位建筑师`;
  }
  filters.forEach(button => button.addEventListener('click', () => filter(button.dataset.filter)));
  function revealHash() {
    const id = location.hash.slice(1);
    if (photos[id]) { filter('all'); requestAnimationFrame(() => document.getElementById(id).scrollIntoView()); }
  }
  window.addEventListener('hashchange', revealHash);
  revealHash();
  const dialog = document.getElementById('lightbox');
  document.querySelectorAll('[data-photo]').forEach(button => button.addEventListener('click', () => {
    const data = photos[button.dataset.photo];
    const img = document.getElementById('large-image');
    img.src = data.src; img.alt = `${data.work}：${data.look}`;
    document.getElementById('large-title').textContent = `${data.name} · ${data.work}`;
    document.getElementById('large-look').textContent = data.look;
    document.getElementById('large-credit').textContent = data.credit;
    document.getElementById('large-source').href = data.source;
    dialog.showModal(); document.body.style.overflow = 'hidden';
  }));
  dialog.querySelector('.close').addEventListener('click', () => dialog.close());
  dialog.addEventListener('click', event => { if (event.target === dialog) { const b = dialog.getBoundingClientRect(); if (event.clientX < b.left || event.clientX > b.right || event.clientY < b.top || event.clientY > b.bottom) dialog.close(); } });
  dialog.addEventListener('close', () => { document.body.style.overflow = ''; });
  const toast = document.getElementById('toast');
  let timer;
  function notify(message) { toast.textContent = message; toast.classList.add('show'); clearTimeout(timer); timer = setTimeout(() => toast.classList.remove('show'), 2600); }
  document.getElementById('share').addEventListener('click', async () => {
    const url = 'https://feixiong.me/arkevo/architecture-vi/' + location.hash;
    try {
      if (navigator.share && matchMedia('(max-width: 720px)').matches) await navigator.share({ title: '20 位建筑师的视觉语言 · arkevo', url });
      else { await navigator.clipboard.writeText(url); notify('分享链接已复制'); }
    } catch (error) {
      if (error.name !== 'AbortError') { window.prompt('复制分享链接', url); }
    }
  });
})();
