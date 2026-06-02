const puppeteer = require('puppeteer-core');
const fs = require('fs');
const path = require('path');

const edgePath = 'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe';
const files = ['HIMS', 'design-guide', 'developer-guide', 'user-guide'];

async function capture() {
  console.log('Launching Microsoft Edge headlessly...');
  const browser = await puppeteer.launch({
    executablePath: edgePath,
    headless: true,
    args: ['--no-sandbox', '--disable-gpu']
  });

  const page = await browser.newPage();
  // Set viewport width large enough to capture the layout correctly
  await page.setViewport({ width: 1200, height: 1000 });

  for (const filename of files) {
    const htmlPath = path.resolve(__dirname, `${filename}.html`);
    const outputDir = path.resolve(__dirname, `${filename}_screenshots`);

    if (!fs.existsSync(outputDir)) {
      fs.mkdirSync(outputDir, { recursive: true });
    }

    console.log(`\nLoading ${filename}.html...`);
    const fileUrl = `file:///${htmlPath.replace(/\\/g, '/')}`;
    await page.goto(fileUrl, { waitUntil: 'load', timeout: 30000 });
    
    // Give external fonts and layouts a moment to fully render
    await new Promise(resolve => setTimeout(resolve, 1500));

    // Find all section elements with class guide-page
    const sections = await page.$$('section.guide-page');
    console.log(`Found ${sections.length} sections in ${filename}.html`);

    for (let i = 0; i < sections.length; i++) {
      const section = sections[i];
      
      // Determine file name based on ID or class
      let name = await page.evaluate(el => el.id, section);
      if (!name) {
        const classes = await page.evaluate(el => el.className, section);
        const cleanClasses = classes.replace('guide-page', '').trim().replace(/\s+/g, '_');
        name = cleanClasses || `section_${i + 1}`;
      }

      // Add a 2-digit index prefix to preserve order in folder views
      const indexStr = String(i + 1).padStart(2, '0');
      const filenamePng = `${indexStr}_${name}.png`;
      const outputPath = path.join(outputDir, filenamePng);

      console.log(`  Taking screenshot of section ${i + 1}/${sections.length} -> ${filenamePng}`);
      try {
        await section.screenshot({ path: outputPath });
      } catch (err) {
        console.error(`  Failed to take screenshot of section ${i + 1}: ${err.message}`);
      }
    }
  }

  await browser.close();
  console.log('\nAll screenshots completed successfully!');
}

capture().catch(err => {
  console.error('Error during capture:', err);
  process.exit(1);
});
