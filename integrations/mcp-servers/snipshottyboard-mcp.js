#!/usr/bin/env node

import { Server } from '@modelcontextprotocol/sdk/server/index.js';
import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio.js';
import { CallToolRequestSchema, ListToolsRequestSchema } from '@modelcontextprotocol/sdk/types.js';
import fs from 'fs/promises';
import { existsSync } from 'fs';
import path from 'path';
import { execSync } from 'child_process';
import { fileURLToPath } from 'url';

// Resolve PROJECT_ROOT dynamically: env var > script location > throw
const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const PROJECT_ROOT = process.env.SSB_PROJECT_ROOT ?? path.resolve(__dirname, '..', '..');

class SnipShottyBoardMCPServer {
  constructor() {
    this.server = new Server(
      { name: 'snipshottyboard-mcp', version: '1.0.1' },
      { capabilities: { tools: {} } }
    );
    this.setupToolHandlers();
  }

  setupToolHandlers() {
    this.server.setRequestHandler(ListToolsRequestSchema, async () => ({
      tools: [
        {
          name: 'read_file',
          description: 'Read contents of a file in the project',
          inputSchema: {
            type: 'object',
            properties: { path: { type: 'string', description: 'Path to file relative to project root' } },
            required: ['path']
          }
        },
        {
          name: 'write_file',
          description: 'Write content to a file in the project',
          inputSchema: {
            type: 'object',
            properties: {
              path: { type: 'string', description: 'Path to file relative to project root' },
              content: { type: 'string', description: 'Content to write to file' }
            },
            required: ['path', 'content']
          }
        },
        {
          name: 'list_directory',
          description: 'List contents of a directory in the project',
          inputSchema: {
            type: 'object',
            properties: { path: { type: 'string', description: 'Directory path relative to project root', default: '.' } }
          }
        },
        {
          name: 'find_files',
          description: 'Find files matching a pattern (FIXED - supports wildcards like *.cs)',
          inputSchema: {
            type: 'object',
            properties: {
              pattern: { type: 'string', description: 'File pattern to search for' },
              directory: { type: 'string', description: 'Directory to search in', default: '.' }
            },
            required: ['pattern']
          }
        },
        {
          name: 'search_code',
          description: 'Search for code patterns across the project',
          inputSchema: {
            type: 'object',
            properties: {
              query: { type: 'string', description: 'Search query' },
              file_types: { type: 'array', items: { type: 'string' }, description: 'File extensions to search', default: ['cs', 'xaml', 'json', 'md'] }
            },
            required: ['query']
          }
        },
        {
          name: 'analyze_csharp_code',
          description: 'Analyze C# code for issues and patterns',
          inputSchema: {
            type: 'object',
            properties: { file_path: { type: 'string', description: 'Path to C# file to analyze' } },
            required: ['file_path']
          }
        },
        {
          name: 'build_wpf_app',
          description: 'Build the WPF application (FIXED - detects running apps)',
          inputSchema: {
            type: 'object',
            properties: {
              configuration: { type: 'string', description: 'Build configuration', default: 'Debug' }
            }
          }
        },
        {
          name: 'run_wpf_app',
          description: 'Run the WPF application',
          inputSchema: { type: 'object', properties: {} }
        },
        {
          name: 'check_dotnet_info',
          description: 'Check .NET SDK info (FIXED - avoids problematic commands)',
          inputSchema: { type: 'object', properties: {} }
        },
        {
          name: 'run_command',
          description: 'Execute a shell command',
          inputSchema: {
            type: 'object',
            properties: {
              command: { type: 'string', description: 'Command to execute' },
              cwd: { type: 'string', description: 'Working directory', default: PROJECT_ROOT }
            },
            required: ['command']
          }
        },
        {
          name: 'validate_json',
          description: 'Validate JSON files (note data, settings, etc.)',
          inputSchema: {
            type: 'object',
            properties: {
              file_path: { type: 'string', description: 'Path to JSON file to validate' }
            },
            required: ['file_path']
          }
        },
        {
          name: 'git_status',
          description: 'Get git status and recent commits',
          inputSchema: { type: 'object', properties: {} }
        },
        {
          name: 'generate_docs',
          description: 'Generate documentation from code comments',
          inputSchema: {
            type: 'object',
            properties: {
              output_format: { type: 'string', description: 'Output format (md/html)', default: 'md' }
            }
          }
        }
      ]
    }));

    this.server.setRequestHandler(CallToolRequestSchema, async (request) => {
      const { name, arguments: args } = request.params;
      try {
        switch (name) {
          case 'read_file': return await this.readFile(args.path);
          case 'write_file': return await this.writeFile(args.path, args.content);
          case 'list_directory': return await this.listDirectory(args.path || '.');
          case 'find_files': return await this.findFiles(args.pattern, args.directory);
          case 'search_code': return await this.searchCode(args.query, args.file_types);
          case 'analyze_csharp_code': return await this.analyzeCSharpCode(args.file_path);
          case 'build_wpf_app': return await this.buildWpfApp(args.configuration);
          case 'run_wpf_app': return await this.runWpfApp();
          case 'check_dotnet_info': return await this.checkDotnetInfo();
          case 'run_command': return await this.runCommand(args.command, args.cwd);
          case 'validate_json': return await this.validateJson(args.file_path);
          case 'git_status': return await this.gitStatus();
          case 'generate_docs': return await this.generateDocs(args.output_format);
          default: throw new Error(`Unknown tool: ${name}`);
        }
      } catch (error) {
        return {
          content: [{ type: 'text', text: `Error executing ${name}: ${error.message}` }],
          isError: true
        };
      }
    });
  }

  async readFile(filePath) {
    const fullPath = path.resolve(PROJECT_ROOT, filePath);
    if (!fullPath.startsWith(PROJECT_ROOT)) throw new Error('Access denied');
    if (!existsSync(fullPath)) throw new Error(`File not found: ${filePath}`);
    const content = await fs.readFile(fullPath, 'utf-8');
    const stats = await fs.stat(fullPath);
    return {
      content: [{
        type: 'text',
        text: `File: ${filePath}\nSize: ${stats.size} bytes\nModified: ${stats.mtime.toISOString()}\n\n${content}`
      }]
    };
  }

  async writeFile(filePath, content) {
    const fullPath = path.resolve(PROJECT_ROOT, filePath);
    if (!fullPath.startsWith(PROJECT_ROOT)) throw new Error('Access denied');
    const dir = path.dirname(fullPath);
    await fs.mkdir(dir, { recursive: true });
    await fs.writeFile(fullPath, content, 'utf-8');
    const stats = await fs.stat(fullPath);
    return {
      content: [{ type: 'text', text: `Successfully wrote ${stats.size} bytes to ${filePath}` }]
    };
  }

  async listDirectory(dirPath) {
    const fullPath = path.resolve(PROJECT_ROOT, dirPath);
    if (!fullPath.startsWith(PROJECT_ROOT)) throw new Error('Access denied');
    if (!existsSync(fullPath)) throw new Error(`Directory not found: ${dirPath}`);
    const entries = await fs.readdir(fullPath, { withFileTypes: true });
    const items = entries.map(entry => ({
      name: entry.name,
      type: entry.isDirectory() ? 'directory' : 'file'
    }));
    return {
      content: [{
        type: 'text',
        text: `Directory: ${dirPath}\n\n${items.map(item => `${item.type === 'directory' ? '📁' : '📄'} ${item.name}`).join('\n')}`
      }]
    };
  }

  // FIXED FILE PATTERN SEARCH - Now the only version
  async findFiles(pattern, directory = '.') {
    try {
      const searchDir = path.resolve(PROJECT_ROOT, directory);
      const results = [];

      // Convert wildcard pattern to regex - PROPERLY FIXED
      const wildcardToRegex = (pattern) => {
        // Escape special regex chars except * and ?
        let regexPattern = pattern.replace(/[.+^${}()|[\]\\]/g, '\\$&');
        // Convert wildcards to regex
        regexPattern = regexPattern.replace(/\\\*/g, '.*').replace(/\\\?/g, '.');
        return new RegExp(`^${regexPattern}$`, 'i');
      };

      let patternRegex = null;
      try {
        patternRegex = wildcardToRegex(pattern);
      } catch (e) {
        // Fallback to simple string matching if regex fails
      }

      const searchRecursive = async (dir) => {
        const entries = await fs.readdir(dir, { withFileTypes: true });

        for (const entry of entries) {
          const fullPath = path.join(dir, entry.name);

          // Skip these directories
          if (['node_modules', '.git', 'bin', 'obj', 'mcp-servers'].includes(entry.name)) continue;

          if (entry.isDirectory()) {
            await searchRecursive(fullPath);
          } else {
            let matches = false;

            // Simple string matching (case-insensitive)
            if (entry.name.toLowerCase().includes(pattern.toLowerCase())) {
              matches = true;
            }

            // Regex pattern matching (if valid)
            if (!matches && patternRegex && patternRegex.test(entry.name)) {
              matches = true;
            }

            if (matches) {
              results.push(path.relative(PROJECT_ROOT, fullPath));
            }
          }
        }
      };

      await searchRecursive(searchDir);

      return {
        content: [{
          type: 'text',
          text: `Files matching "${pattern}":\n\n${results.join('\n') || 'No matches found'}`
        }]
      };
    } catch (error) {
      return {
        content: [{ type: 'text', text: `File search failed: ${error.message}` }],
        isError: true
      };
    }
  }

  async searchCode(query, fileTypes = ['cs', 'xaml', 'json', 'md']) {
    try {
      const results = [];
      const searchDir = async (dir) => {
        const entries = await fs.readdir(dir, { withFileTypes: true });
        for (const entry of entries) {
          const fullPath = path.join(dir, entry.name);
          if (['node_modules', '.git', 'bin', 'obj', '.vs', 'mcp-servers'].includes(entry.name)) continue;

          if (entry.isDirectory()) {
            await searchDir(fullPath);
          } else {
            const ext = path.extname(entry.name).slice(1);
            if (fileTypes.includes(ext)) {
              try {
                const content = await fs.readFile(fullPath, 'utf-8');
                if (content.includes(query)) {
                  const relativePath = path.relative(PROJECT_ROOT, fullPath);
                  const lines = content.split('\n');
                  const matchingLines = lines
                    .map((line, index) => ({ line, number: index + 1 }))
                    .filter(({ line }) => line.includes(query))
                    .slice(0, 3);
                  results.push({ file: relativePath, matches: matchingLines });
                }
              } catch (e) {
                // Skip files we can't read
              }
            }
          }
        }
      };

      await searchDir(PROJECT_ROOT);
      const formattedResults = results.map(result =>
        `${result.file}:\n${result.matches.map(m => `  Line ${m.number}: ${m.line.trim()}`).join('\n')}`
      ).join('\n\n');

      return {
        content: [{
          type: 'text',
          text: `Search results for "${query}" in ${fileTypes.join(', ')} files:\n\n${formattedResults || 'No matches found'}`
        }]
      };
    } catch (error) {
      return {
        content: [{ type: 'text', text: `Search error: ${error.message}` }],
        isError: true
      };
    }
  }

  async analyzeCSharpCode(filePath) {
    try {
      const fullPath = path.resolve(PROJECT_ROOT, filePath);
      const content = await fs.readFile(fullPath, 'utf-8');

      const analysis = {
        lines: content.split('\n').length,
        classes: (content.match(/class\s+\w+/g) || []).length,
        methods: (content.match(/\w+\s+\w+\s*\([^)]*\)\s*{/g) || []).length,
        properties: (content.match(/\w+\s+\w+\s*{\s*get/g) || []).length,
        usings: (content.match(/using\s+[\w.]+;/g) || []).length,
        todos: (content.match(/\/\/\s*TODO.*/gi) || []).length,
        fixmes: (content.match(/\/\/\s*FIXME.*/gi) || []).length
      };

      return {
        content: [{
          type: 'text',
          text: `C# Code Analysis for ${filePath}:\n\nLines of code: ${analysis.lines}\nClasses: ${analysis.classes}\nMethods: ${analysis.methods}\nProperties: ${analysis.properties}\nUsing statements: ${analysis.usings}\nTODOs: ${analysis.todos}\nFIXMEs: ${analysis.fixmes}`
        }]
      };
    } catch (error) {
      return {
        content: [{ type: 'text', text: `Analysis failed: ${error.message}` }],
        isError: true
      };
    }
  }

  async buildWpfApp(configuration = 'Debug') {
    try {
      const isRunning = await this.checkIfAppRunning();
      if (isRunning) {
        return {
          content: [{
            type: 'text',
            text: `Build skipped: SnipShottyBoard is currently running.\n\nPlease close the application first, then try building again.`
          }]
        };
      }

      const result = execSync(`dotnet build --configuration ${configuration} --verbosity minimal`, {
        cwd: PROJECT_ROOT,
        encoding: 'utf-8',
        shell: true,
        timeout: 60000
      });
      return {
        content: [{ type: 'text', text: `Build successful (${configuration}):\n\n${result}` }]
      };
    } catch (error) {
      return {
        content: [{ type: 'text', text: `Build failed:\n\n${error.message}` }],
        isError: true
      };
    }
  }

  async runWpfApp() {
    try {
      const isRunning = await this.checkIfAppRunning();
      if (isRunning) {
        return {
          content: [{ type: 'text', text: 'SnipShottyBoard is already running!' }]
        };
      }

      const exePath = path.join(PROJECT_ROOT, 'bin', 'Debug', 'net8.0-windows', 'SnipShottyBoard.exe');
      if (existsSync(exePath)) {
        execSync(`start "" "${exePath}"`, { cwd: PROJECT_ROOT, shell: true });
        return {
          content: [{ type: 'text', text: 'WPF application started successfully' }]
        };
      } else {
        execSync('start "" dotnet run', { cwd: PROJECT_ROOT, shell: true });
        return {
          content: [{ type: 'text', text: 'WPF application started via dotnet run' }]
        };
      }
    } catch (error) {
      return {
        content: [{ type: 'text', text: `Error starting app:\n\n${error.message}` }],
        isError: true
      };
    }
  }

  async checkIfAppRunning() {
    try {
      const result = execSync('tasklist /FI "IMAGENAME eq SnipShottyBoard.exe"', {
        encoding: 'utf-8',
        shell: true,
        timeout: 5000
      });
      return result.includes('SnipShottyBoard.exe');
    } catch (error) {
      return false;
    }
  }

  async checkDotnetInfo() {
    try {
      const version = execSync('dotnet --version', {
        cwd: PROJECT_ROOT,
        encoding: 'utf-8',
        shell: true,
        timeout: 10000
      });

      let sdkInfo = '';
      try {
        sdkInfo = execSync('dotnet --list-sdks', {
          cwd: PROJECT_ROOT,
          encoding: 'utf-8',
          shell: true,
          timeout: 10000
        });
      } catch (e) {
        sdkInfo = 'SDK list unavailable';
      }

      let runtimeInfo = '';
      try {
        runtimeInfo = execSync('dotnet --list-runtimes', {
          cwd: PROJECT_ROOT,
          encoding: 'utf-8',
          shell: true,
          timeout: 10000
        });
      } catch (e) {
        runtimeInfo = 'Runtime list unavailable';
      }

      return {
        content: [{
          type: 'text',
          text: `.NET Information:\n\nVersion: ${version}\n\nSDKs:\n${sdkInfo}\n\nRuntimes:\n${runtimeInfo}`
        }]
      };
    } catch (error) {
      return {
        content: [{
          type: 'text',
          text: `Error getting .NET info: ${error.message}\n\nNote: Basic functionality should still work.`
        }],
        isError: true
      };
    }
  }

  async runCommand(command, cwd = PROJECT_ROOT) {
    try {
      const result = execSync(command, {
        cwd: cwd || PROJECT_ROOT,
        encoding: 'utf-8',
        timeout: 30000,
        shell: true
      });
      return {
        content: [{
          type: 'text',
          text: `Command: ${command}\nDirectory: ${cwd}\n\nOutput:\n${result}`
        }]
      };
    } catch (error) {
      return {
        content: [{
          type: 'text',
          text: `Command: ${command}\nDirectory: ${cwd}\n\nError:\n${error.message}`
        }],
        isError: true
      };
    }
  }

  async validateJson(filePath) {
    try {
      const fullPath = path.resolve(PROJECT_ROOT, filePath);
      const content = await fs.readFile(fullPath, 'utf-8');
      const parsed = JSON.parse(content);

      return {
        content: [{
          type: 'text',
          text: `JSON validation successful for ${filePath}\nObject keys: ${Object.keys(parsed).join(', ')}\nFile size: ${content.length} bytes`
        }]
      };
    } catch (error) {
      return {
        content: [{ type: 'text', text: `JSON validation failed for ${filePath}: ${error.message}` }],
        isError: true
      };
    }
  }

  async gitStatus() {
    try {
      const result = execSync('git status --porcelain', {
        cwd: PROJECT_ROOT,
        encoding: 'utf-8',
        shell: true
      });

      const recentCommits = execSync('git log --oneline -5', {
        cwd: PROJECT_ROOT,
        encoding: 'utf-8',
        shell: true
      });

      return {
        content: [{
          type: 'text',
          text: `Git Status:\n\n${result || 'Working directory clean'}\n\nRecent commits:\n${recentCommits}`
        }]
      };
    } catch (error) {
      return {
        content: [{
          type: 'text',
          text: `Git error: ${error.message}\n\nNote: Make sure Git is installed and repo is initialized.`
        }],
        isError: true
      };
    }
  }

  async generateDocs(outputFormat = 'md') {
    try {
      const docs = [];

      const processFile = async (filePath) => {
        const content = await fs.readFile(filePath, 'utf-8');
        const lines = content.split('\n');

        let currentClass = null;
        let currentComment = [];

        for (let i = 0; i < lines.length; i++) {
          const line = lines[i].trim();

          if (line.startsWith('///')) {
            currentComment.push(line.substring(3).trim());
            continue;
          }

          const classMatch = line.match(/(?:public|private|protected|internal)?\s*class\s+(\w+)/);
          if (classMatch) {
            currentClass = {
              name: classMatch[1],
              comment: currentComment.join(' '),
              methods: []
            };
            docs.push(currentClass);
            currentComment = [];
            continue;
          }

          const methodMatch = line.match(/(?:public|private|protected|internal)?\s*(?:static\s+)?(\w+)\s+(\w+)\s*\([^)]*\)/);
          if (methodMatch && currentClass) {
            currentClass.methods.push({
              returnType: methodMatch[1],
              name: methodMatch[2],
              comment: currentComment.join(' ')
            });
            currentComment = [];
            continue;
          }

          if (line && !line.startsWith('///')) {
            currentComment = [];
          }
        }
      };

      // Find all C# files
      const findCSharpFiles = async (dir) => {
        const entries = await fs.readdir(dir, { withFileTypes: true });
        const files = [];

        for (const entry of entries) {
          const fullPath = path.join(dir, entry.name);
          if (['bin', 'obj', '.git', 'mcp-servers'].includes(entry.name)) continue;

          if (entry.isDirectory()) {
            files.push(...await findCSharpFiles(fullPath));
          } else if (entry.name.endsWith('.cs')) {
            files.push(fullPath);
          }
        }
        return files;
      };

      const csharpFiles = await findCSharpFiles(PROJECT_ROOT);
      for (const file of csharpFiles) {
        await processFile(file);
      }

      // Generate markdown
      let markdown = '# SnipShottyBoard API Documentation\n\n';
      markdown += `Generated on: ${new Date().toISOString()}\n\n`;

      for (const classInfo of docs) {
        if (classInfo.name) {
          markdown += `## ${classInfo.name}\n\n`;
          if (classInfo.comment) {
            markdown += `${classInfo.comment}\n\n`;
          }

          if (classInfo.methods && classInfo.methods.length > 0) {
            markdown += '### Methods\n\n';
            for (const method of classInfo.methods) {
              markdown += `#### ${method.name}\n\n`;
              if (method.comment) {
                markdown += `${method.comment}\n\n`;
              }
              markdown += `**Returns:** ${method.returnType}\n\n`;
            }
          }
        }
      }

      // Save documentation
      const docsPath = path.join(PROJECT_ROOT, 'docs', `api.${outputFormat}`);
      await fs.mkdir(path.dirname(docsPath), { recursive: true });
      await fs.writeFile(docsPath, markdown, 'utf-8');

      return {
        content: [{
          type: 'text',
          text: `Documentation generated: ${docsPath}\n\nProcessed ${csharpFiles.length} C# files\nFound ${docs.length} classes`
        }]
      };
    } catch (error) {
      return {
        content: [{ type: 'text', text: `Documentation generation failed: ${error.message}` }],
        isError: true
      };
    }
  }

  async run() {
    const transport = new StdioServerTransport();
    await this.server.connect(transport);
    setInterval(() => { }, 1 << 30);
  }
}

const server = new SnipShottyBoardMCPServer();
server.run().catch(console.error);