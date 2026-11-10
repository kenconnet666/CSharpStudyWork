// 学习站的字体（取自 Rune 学习站的字体切换）：默认系统字体（主题的字体栈：苹方、微软雅黑……），顶栏的字体按钮可以换成
// 等距更纱黑体 SC、霞鹜文楷等宽（npm 包 rune-ui-font，按字切成小片）。选择记在 localStorage；网页字体选到时才从 CDN 加载，
// 到之前先用系统字体。字体变量的覆盖在 app.css（按根元素的 data-learn-font）。
// head 里同步加载：页面显示前就按记住的选择设好，不闪一下系统字体的排版；示例的 iframe 跟着 storage 事件一起换。
window.learnFonts = (() => {
  const key = 'learnsite-font';
  const sheets = { sarasa: 'sarasa-mono-sc.css', wenkai: 'lxgw-wenkai-mono.css' };

  function read() {
    try {
      return localStorage.getItem(key) ?? 'system';
    } catch {
      return 'system';
    }
  }

  function apply(name) {
    const root = document.documentElement;
    if (!(name in sheets)) {
      delete root.dataset.learnFont;
      return;
    }
    root.dataset.learnFont = name;
    const href = `https://cdn.jsdelivr.net/npm/rune-ui-font@0.2.1/${sheets[name]}`;
    if (!document.querySelector(`link[href="${href}"]`)) {
      document.head.append(Object.assign(document.createElement('link'), { rel: 'stylesheet', href }));
    }
  }

  apply(read());
  addEventListener('storage', (event) => event.key === key && apply(read()));

  return {
    get: read,
    set(name) {
      try {
        localStorage.setItem(key, name);
      } catch {
        // 存不下（隐私模式）也照样换，只是不记住
      }
      apply(name);
    },
  };
})();
