// The page's own title survives the game's start-up; a page without one gets the game's name as written. Run twice by run-web-tests.sh:
// once on a page with <title>, once on one without (EXPECT_TITLE in the page's URL query names the title wanted).
export default async function ({ evalJs, sleep }) {
  for (let i = 0; i < 300 && !(await evalJs('window.__onegame && window.__onegame.frames > 5')); i++) await sleep(100);
  const want = await evalJs('new URLSearchParams(location.search).get("expect")');
  const title = await evalJs('document.title');
  return { ok: title === want, title, want };
}
