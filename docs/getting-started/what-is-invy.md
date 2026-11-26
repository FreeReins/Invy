# What Invy does

Invy keeps track of what you own and where it is: the drill in the blue box on the second shelf of the shed, the
spare HDMI cables, the warranty for the dishwasher, the 40 jars of screws in the workshop. It runs on your own
computer or home server, so your list of belongings stays with you.

## The problems it solves

- **"Where did I put it?"** Search finds anything by name, type, field or place as you type, and shows its whole
  location: *Shed › Shelf 2 › Box ABC*.
- **Boxes inside boxes.** Places and containers nest as deep as you like. Move a box and everything in it moves too.
- **Typing everything in.** Labels with QR codes, barcode lookups and photos read by an AI model fill in the details
  for you.
- **Different kinds of things.** Each item type has its own fields: a tool's serial number, a cable's length, a
  book's author.
- **Sharing.** One install holds several spaces, such as Home, Workshop and Shop, each with its own members.
- **Assistants.** Claude and other assistants that support MCP can search and update your inventory for you.

## Who it's for

- **A household or one person** keeping track of their things.
- **Hobbyists and makers** with parts, tools and materials.
- **Small businesses** with stock and equipment.
- **People who like to run their own services.** Invy is one container, or one Windows program, with its PostgreSQL
  database inside. It runs anywhere Docker or Podman does.

## What it is not

- Not a cloud service. Nothing is sent anywhere unless you set it up: barcode lookups, a webhook or an AI model.
- Not accounting or point-of-sale software.

## Next

[Check what you need](requirements.md), then install Invy.
