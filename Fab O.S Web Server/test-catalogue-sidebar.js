const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch({ headless: false });
  const context = await browser.newContext();
  const page = await context.newPage();

  console.log('🚀 Navigating to application...');
  await page.goto('http://localhost:5223');

  console.log('🔐 Logging in...');
  // Wait for login page
  await page.waitForSelector('input[name="Email"], input[type="email"]', { timeout: 10000 });

  // Fill in login credentials
  await page.fill('input[name="Email"], input[type="email"]', 'admin@steelestimation.com');
  await page.fill('input[name="Password"], input[type="password"]', 'Admin@123');

  // Click login button
  await page.click('button[type="submit"]');

  console.log('⏳ Waiting for dashboard...');
  await page.waitForTimeout(3000);

  console.log('📋 Navigating to Package SharePoint Files page...');
  await page.goto('http://localhost:5223/packages/16/sharepoint-files');
  await page.waitForTimeout(3000);

  console.log('📄 Taking screenshot of SharePoint Files page...');
  await page.screenshot({ path: 'sharepoint-files-page.png', fullPage: true });
  console.log('   Screenshot saved to sharepoint-files-page.png');

  console.log('🔍 Looking for a PDF to open...');
  // Look for and DOUBLE-click a PDF link
  const pdfLink = await page.locator('a[href*=".pdf"], button:has-text("View"), [class*="pdf"], tr, .file-item').first();
  if (await pdfLink.count() > 0) {
    await pdfLink.dblclick();
    await page.waitForTimeout(3000);

    console.log('📸 Taking screenshot after PDF opened...');
    await page.screenshot({ path: 'pdf-opened.png', fullPage: true });
    console.log('   Screenshot saved to pdf-opened.png');

    console.log('📊 Checking body classes before catalogue toggle...');
    const bodyClassesBefore = await page.evaluate(() => document.body.className);
    console.log('   Body classes:', bodyClassesBefore);

    const modalBefore = await page.evaluate(() => {
      const modal = document.querySelector('.modal-fullscreen');
      if (modal) {
        const style = window.getComputedStyle(modal);
        return { left: style.left, zIndex: style.zIndex };
      }
      return null;
    });
    console.log('   Modal position before:', modalBefore);

    console.log('🎯 Clicking the Catalogue button to CLOSE the sidebar...');
    // Click the Catalogue button in the modal toolbar (the blue button with chart icon)
    const catalogueButton = await page.locator('.modal-fullscreen button:has-text("Catalogue")').first();
    console.log('   Catalogue button found, clicking...');
    await catalogueButton.click();
    await page.waitForTimeout(2000); // Wait for Blazor SignalR update

    console.log('📊 Checking body classes after catalogue closed...');
    const bodyClassesAfterClose = await page.evaluate(() => document.body.className);
    console.log('   Body classes:', bodyClassesAfterClose);

    const modalAfterClose = await page.evaluate(() => {
      const modal = document.querySelector('.modal-fullscreen');
      if (modal) {
        const style = window.getComputedStyle(modal);
        return { left: style.left, zIndex: style.zIndex };
      }
      return null;
    });
    console.log('   Modal position after close:', modalAfterClose);

    console.log('🔄 Clicking Catalogue button again to REOPEN the sidebar...');
    await catalogueButton.click();
    await page.waitForTimeout(2000); // Wait for Blazor SignalR update

    console.log('📸 Taking final screenshot...');
    await page.screenshot({ path: 'final-state.png', fullPage: true });

    console.log('📊 Checking body classes after catalogue reopened...');
    const bodyClassesAfterReopen = await page.evaluate(() => document.body.className);
    console.log('   Body classes:', bodyClassesAfterReopen);

    const modalAfterReopen = await page.evaluate(() => {
      const modal = document.querySelector('.modal-fullscreen');
      if (modal) {
        const style = window.getComputedStyle(modal);
        return { left: style.left, zIndex: style.zIndex };
      }
      return null;
    });
    console.log('   Modal position after reopen:', modalAfterReopen);

    console.log('\n✅ Test Results:');
    console.log('   - Modal-open class present?', bodyClassesAfterReopen.includes('modal-open'));
    console.log('   - Catalogue-sidebar-open class present after reopen?', bodyClassesAfterReopen.includes('catalogue-sidebar-open'));
    console.log('   - Catalogue-sidebar-open class was removed when closed?', !bodyClassesAfterClose.includes('catalogue-sidebar-open'));

    // When catalogue is closed, modal should be at 280px (default sidebar)
    const expectedLeftWhenClosed = bodyClassesAfterClose.includes('sidebar-collapsed') ? '60px' : '280px';
    const actualLeftWhenClosed = modalAfterClose?.left;

    // When catalogue is open, modal should be at 600px (default sidebar + catalogue)
    const expectedLeftWhenOpen = bodyClassesAfterReopen.includes('sidebar-collapsed') ? '380px' : '600px';
    const actualLeftWhenOpen = modalAfterReopen?.left;

    console.log(`\n📊 When Catalogue CLOSED:`);
    console.log(`   - Expected modal left: ${expectedLeftWhenClosed}`);
    console.log(`   - Actual modal left: ${actualLeftWhenClosed}`);
    console.log(`   - Positioned correctly? ${actualLeftWhenClosed === expectedLeftWhenClosed ? '✅ YES' : '❌ NO'}`);

    console.log(`\n📊 When Catalogue REOPENED:`);
    console.log(`   - Expected modal left: ${expectedLeftWhenOpen}`);
    console.log(`   - Actual modal left: ${actualLeftWhenOpen}`);
    console.log(`   - Positioned correctly? ${actualLeftWhenOpen === expectedLeftWhenOpen ? '✅ YES' : '❌ NO'}`);

    const closedCorrect = actualLeftWhenClosed === expectedLeftWhenClosed;
    const openCorrect = actualLeftWhenOpen === expectedLeftWhenOpen;

    if (!closedCorrect || !openCorrect) {
      console.log('\n❌ FAILED: Modal did not reposition correctly!');
      console.log('   The catalogue sidebar is likely overlapping the modal.');
    } else {
      console.log('\n✅ PASSED: Modal repositioned correctly!');
      console.log('   The catalogue sidebar toggle works perfectly - no overlap!');
    }

    // Keep browser open for 10 seconds to visually verify
    console.log('\n⏳ Keeping browser open for 10 seconds for visual verification...');
    await page.waitForTimeout(10000);
  } else {
    console.log('❌ Could not find a PDF to open');
  }

  await browser.close();
  console.log('\n✨ Test complete!');
})();
