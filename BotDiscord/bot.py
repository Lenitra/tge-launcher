import os
import asyncio
from dotenv import load_dotenv
import discord
from discord.ext import commands

load_dotenv()
TOKEN = os.getenv('DISCORD_TOKEN')

intents = discord.Intents.default()
intents.message_content = True

bot = commands.Bot(command_prefix='!', intents=intents)


@bot.event
async def on_ready():
    print(f'Logged in as {bot.user} (ID: {bot.user.id})')


@bot.command(name='ping')
async def ping(ctx):
    """Répond 'Pong!' et latence en ms."""
    latency_ms = round(bot.latency * 1000)
    await ctx.send(f'Pong! Latency: {latency_ms}ms')


if __name__ == '__main__':
    if not TOKEN:
        print('DISCORD_TOKEN not found. Create a .env file with DISCORD_TOKEN=your_token')
        raise SystemExit(1)

    try:
        bot.run(TOKEN)
    except KeyboardInterrupt:
        print('Exiting...')
    except Exception as e:
        print('Failed to start bot:', e)
