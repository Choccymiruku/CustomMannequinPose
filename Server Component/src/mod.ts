/* eslint-disable no-mixed-spaces-and-tabs */
/* eslint-disable @typescript-eslint/indent */
/* eslint-disable @typescript-eslint/naming-convention */
import { DependencyContainer } from "tsyringe";
import { IPostDBLoadMod } from "@spt/models/external/IPostDBLoadMod";
import { CustomItemService } from "@spt/services/mod/CustomItemService";
import { DatabaseServer } from "@spt/servers/DatabaseServer";
import { CreateProfileService } from "@spt/services/CreateProfileService";
import { IPostSptLoadMod } from "@spt/models/external/IPostSptLoadMod";
import { IPreSptLoadMod } from "@spt/models/external/IPreSptLoadMod"

import Poses from "../src/PoseList.json";
import { ProfileHelper } from "@spt/helpers/ProfileHelper";
import { StaticRouterModService } from "@spt/services/mod/staticRouter/StaticRouterModService";

class Mod implements IPostDBLoadMod, IPreSptLoadMod
{
	public postDBLoad(container: DependencyContainer): void
	{
		const customitem = container.resolve<CustomItemService>("CustomItemService");
		const databaseServer = container.resolve<DatabaseServer>("DatabaseServer");
		const db = databaseServer.getTables();
		const globals = db.globals;
		const locales = db.locales.global;
		const poseName: Record<string, string> = Poses.Name

		//insert localization to all language
		for (const lcl in locales)
		{
			for (const names in poseName)
			{
				locales[lcl][names] = poseName[names]
			}
		}

		//insert the poses into customization
		for (const gesture in Poses.Poses)
		{
			db.templates.customization[gesture] = Poses.Poses[gesture]
		}

	}

	
	//some fuckery to add the poses so it can be used by the player
	//i have 0 fucking clue why you need to manually add them to
	//player inventory when they are unlocked by default, BSG moment
	public preSptLoad(container: DependencyContainer): void
	{
		const staticRouteService = container.resolve<StaticRouterModService>("StaticRouterModService");
		staticRouteService.registerStaticRouter(
			"AddPoseToUser",
			[
				{
					url: "/client/game/start",
					action: async (url, info, sessionId, output) =>
					{
						const profileHelper = container.resolve<ProfileHelper>("ProfileHelper")
						const userList = profileHelper.getProfiles();
						const poseUnlock = Poses.PoseUnlock;
						//loop through all user
						for (const user in userList)
						{
							//loop through all unlock
							for (const unlock of poseUnlock)
							{
								//check if profile has the customisation array or not
								//edge case since creating profile doesn't have it
								if (userList[user].customisationUnlocks)
								{
									//avoid adding the same pose unlock multiple time
									const exist = userList[user].customisationUnlocks.some(x => x.id === unlock.id);
									if (!exist)
									{
										userList[user].customisationUnlocks.push(...poseUnlock);
									}
								}
							}
						}
						return output
					}
				}
			],
			"spt"
		)
		
	}
}

export const mod = new Mod();
