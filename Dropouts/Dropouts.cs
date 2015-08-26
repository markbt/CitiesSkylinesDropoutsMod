using ICities;
using ColossalFramework;
using UnityEngine;

namespace Dropouts
{
    public class Flunker : ThreadingExtensionBase
    {
        // ResidentAI subclass to access protected methods.
        public class FlunkingResidentAI : ResidentAI
        {
            public void LeaveSchool(uint citizenIndex, CitizenInfo citizenInfo)
            {
                CitizenManager citizenManager = Singleton<CitizenManager>.instance;

                m_info = citizenInfo;

                if (citizenManager.m_citizens.m_buffer[citizenIndex].m_workBuilding != 0)
                {
                    if (citizenManager.m_citizens.m_buffer[citizenIndex].CurrentLocation == Citizen.Location.Work && citizenManager.m_citizens.m_buffer[citizenIndex].m_homeBuilding != 0)
                    {
                        StartMoving(citizenIndex, ref citizenManager.m_citizens.m_buffer[citizenIndex], citizenManager.m_citizens.m_buffer[citizenIndex].m_workBuilding, citizenManager.m_citizens.m_buffer[citizenIndex].m_homeBuilding);
                    }

                    BuildingManager buildingManager = Singleton<BuildingManager>.instance;
                    uint unitIndex = buildingManager.m_buildings.m_buffer[citizenManager.m_citizens.m_buffer[citizenIndex].m_workBuilding].m_citizenUnits;
                    citizenManager.m_citizens.m_buffer[citizenIndex].RemoveFromUnits(citizenIndex, unitIndex, CitizenUnit.Flags.Student | CitizenUnit.Flags.Work);
                    citizenManager.m_citizens.m_buffer[citizenIndex].m_workBuilding = 0;
                }
                citizenManager.m_citizens.m_buffer[citizenIndex].m_flags &= ~Citizen.Flags.Student;

                m_info = null;
            }

            public void UpdateEducationLevel(uint citizenIndex, int grade)
            {
                CitizenManager citizenManager = Singleton<CitizenManager>.instance;

                if (grade >= 0)
                {
                    citizenManager.m_citizens.m_buffer[citizenIndex].Education1 = (grade >= 15);
                    citizenManager.m_citizens.m_buffer[citizenIndex].Education2 = (grade >= 35);
                }
                citizenManager.m_citizens.m_buffer[citizenIndex].Education3 = false;

                #if DROPOUTS_DEBUG
                Debug.Log(String.Format("Citizen {0} is now {1} {2} {3}: {4}",
                    citizenIndex,
                    citizenManager.m_citizens.m_buffer[citizenIndex].Education1,
                    citizenManager.m_citizens.m_buffer[citizenIndex].Education2,
                    citizenManager.m_citizens.m_buffer[citizenIndex].Education3,
                    citizenManager.m_citizens.m_buffer[citizenIndex].EducationLevel));
                #endif
            }

            public void RemoveSchoolOffers(uint citizenIndex)
            {
                // Actually we're going to look for any outstanding job/student transfer offers for this person and remove them
                CitizenManager citizenManager = Singleton<CitizenManager>.instance;
                TransferManager transferManager = Singleton<TransferManager>.instance;
                var offer = default(TransferManager.TransferOffer);
                offer.Citizen = citizenIndex;
                for (var reason = TransferManager.TransferReason.Student1;
                    reason <= TransferManager.TransferReason.Student3;
                    ++reason)
                {
                    transferManager.RemoveOutgoingOffer(reason, offer);
                }
            }

            public void RemoveWorkOffers(uint citizenIndex)
            {
                // Actually we're going to look for any outstanding job/student transfer offers for this person and remove them
                CitizenManager citizenManager = Singleton<CitizenManager>.instance;
                TransferManager transferManager = Singleton<TransferManager>.instance;
                var offer = default(TransferManager.TransferOffer);
                offer.Citizen = citizenIndex;
                for (var reason = TransferManager.TransferReason.Worker0;
                    reason <= TransferManager.TransferReason.Worker3;
                    ++reason)
                {
                    transferManager.RemoveOutgoingOffer(reason, offer);
                }
            }

            public void GetAJob(uint citizenIndex)
            {
                CitizenManager citizenManager = Singleton<CitizenManager>.instance;

                if (citizenManager.m_citizens.m_buffer[citizenIndex].m_workBuilding == 0 && citizenManager.m_citizens.m_buffer[citizenIndex].m_homeBuilding != 0)
                {
                    SimulationManager simulationManager = Singleton<SimulationManager>.instance;
                    BuildingManager buildingManager = Singleton<BuildingManager>.instance;
                    TransferManager transferManager = Singleton<TransferManager>.instance;
                    Vector3 position = buildingManager.m_buildings.m_buffer[citizenManager.m_citizens.m_buffer[citizenIndex].m_homeBuilding].m_position;

                    TransferManager.TransferOffer offer = default(TransferManager.TransferOffer);
                    offer.Priority = simulationManager.m_randomizer.Int32(8);
                    offer.Citizen = citizenIndex;
                    offer.Position = position;
                    offer.Amount = 1;
                    offer.Active = true;
                    switch (citizenManager.m_citizens.m_buffer[citizenIndex].EducationLevel)
                    {
                    case Citizen.Education.Uneducated:
                        transferManager.AddOutgoingOffer(TransferManager.TransferReason.Worker0, offer);
                        break;
                    case Citizen.Education.OneSchool:
                        transferManager.AddOutgoingOffer(TransferManager.TransferReason.Worker1, offer);
                        break;
                    case Citizen.Education.TwoSchools:
                        transferManager.AddOutgoingOffer(TransferManager.TransferReason.Worker2, offer);
                        break;
                    case Citizen.Education.ThreeSchools:
                        transferManager.AddOutgoingOffer(TransferManager.TransferReason.Worker3, offer);
                        break;
                    }
                }
            }
        }

        FlunkingResidentAI m_residentAI;

        public override void OnCreated (IThreading threading)
        {
            m_residentAI = new FlunkingResidentAI();
            #if DROPOUTS_DEBUG
            Debug.Log("Flunker loaded");
            #endif
        }

        public override void OnAfterSimulationFrame()
        {
            SimulationManager simulationManager = Singleton<SimulationManager>.instance;
            CitizenManager citizenManager = Singleton<CitizenManager>.instance;

            uint frameOffset = simulationManager.m_currentFrameIndex & 4095u;
            uint startIndex = frameOffset * 256;
            uint endIndex = (frameOffset + 1) * 256 - 1;
            for (uint index = startIndex; index <= endIndex; ++index)
            {
                if ((citizenManager.m_citizens.m_buffer[index].m_flags & Citizen.Flags.Created) != Citizen.Flags.None)
                {
                    CitizenInfo citizenInfo = citizenManager.m_citizens.m_buffer[index].GetCitizenInfo(index);
                    if (citizenInfo != null && citizenInfo.m_citizenAI is ResidentAI)
                    {
                        if (citizenManager.m_citizens.m_buffer[index].m_age == Citizen.AGE_LIMIT_TEEN)
                        {
                            MaybeFlunkHighSchool(index, citizenInfo);
                        }
                        if (citizenManager.m_citizens.m_buffer[index].m_age == Citizen.AGE_LIMIT_YOUNG)
                        {
                            MaybeFlunkUniversity(index, citizenInfo);
                        }
                        if (citizenManager.m_citizens.m_buffer[index].m_age >= Citizen.AGE_LIMIT_TEEN &&
                            citizenManager.m_citizens.m_buffer[index].m_age < Citizen.AGE_LIMIT_ADULT)
                        {
                            if (citizenManager.m_citizens.m_buffer[index].EducationLevel < Citizen.Education.TwoSchools)
                            {
                                if (citizenManager.m_citizens.m_buffer[index].m_workBuilding != 0 &&
                                    (citizenManager.m_citizens.m_buffer[index].m_flags & Citizen.Flags.Student) != Citizen.Flags.None)
                                {
                                    #if DROPOUTS_DEBUG
                                    Debug.Log(String.Format("Expelling citizen {0} from university for lack of quals", index));
                                    #endif
                                    m_residentAI.LeaveSchool(index, citizenInfo);
                                    m_residentAI.RemoveSchoolOffers(index);
                                    m_residentAI.RemoveWorkOffers(index);
                                    m_residentAI.GetAJob(index);
                                }
                                else if (citizenManager.m_citizens.m_buffer[index].m_workBuilding == 0)
                                {
                                    #if DROPOUTS_DEBUG
                                    Debug.Log(String.Format("Removing citizen {0}'s university applications", index));
                                    #endif
                                    m_residentAI.RemoveSchoolOffers(index);
                                }
                            }
                        }
                    }
                }
            }
        }

        static int TakeExam(uint citizenIndex)
        {
            SimulationManager simulationManager = Singleton<SimulationManager>.instance;
            CitizenManager citizenManager = Singleton<CitizenManager>.instance;
            DistrictManager districtManager = Singleton<DistrictManager>.instance;
            BuildingManager buildingManager = Singleton<BuildingManager>.instance;
            Vector3 position = buildingManager.m_buildings.m_buffer[citizenManager.m_citizens.m_buffer[citizenIndex].m_homeBuilding].m_position;
            byte district = districtManager.GetDistrict(position);
            DistrictPolicies.Services servicePolicies = districtManager.m_districts.m_buffer[(int)district].m_servicePolicies;
            int grade = simulationManager.m_randomizer.Int32(100);
            if ((servicePolicies & DistrictPolicies.Services.EducationBoost) != 0)
            {
                grade += 20;
            }
            return grade;
        }

        void MaybeFlunkHighSchool(uint citizenIndex, CitizenInfo citizenInfo)
        {
            int grade = TakeExam(citizenIndex);
            #if DROPOUTS_DEBUG
            Debug.Log(String.Format("Considering flunking citizen {0} from high school with grade {1}", citizenIndex, grade));
            #endif
            if (grade < 70)
            {
                // This student flunked high school.  Now they need to cut their hair and get a job.
                m_residentAI.LeaveSchool(citizenIndex, citizenInfo);
                m_residentAI.UpdateEducationLevel(citizenIndex, grade);
                m_residentAI.RemoveSchoolOffers(citizenIndex);
                m_residentAI.RemoveWorkOffers(citizenIndex);
                m_residentAI.GetAJob(citizenIndex);
            }
        }

        void MaybeFlunkUniversity(uint citizenIndex, CitizenInfo citizenInfo)
        {
            CitizenManager citizenManager = Singleton<CitizenManager>.instance;

            if (citizenManager.m_citizens.m_buffer[citizenIndex].m_workBuilding != 0 &&
                (citizenManager.m_citizens.m_buffer[citizenIndex].m_flags & Citizen.Flags.Student) == Citizen.Flags.None)
            {
                // Already has a job
                return;
            }

            int grade = TakeExam(citizenIndex);

            #if DROPOUTS_DEBUG
            Debug.Log(String.Format("Considering flunking citizen {0} from university with grade {1} and education {2} {3}",
                citizenIndex,
                grade,
                citizenManager.m_citizens.m_buffer[citizenIndex].Education1,
                citizenManager.m_citizens.m_buffer[citizenIndex].Education2));
            #endif

            if (!citizenManager.m_citizens.m_buffer[citizenIndex].Education1 ||
                !citizenManager.m_citizens.m_buffer[citizenIndex].Education2 ||
                grade < 50)
            {
                // This student flunked university.  Now they need to cut their hair and get a job.
                m_residentAI.LeaveSchool(citizenIndex, citizenInfo);
                m_residentAI.UpdateEducationLevel(citizenIndex, -1);
                m_residentAI.RemoveSchoolOffers(citizenIndex);
                m_residentAI.RemoveWorkOffers(citizenIndex);
                m_residentAI.GetAJob(citizenIndex);
            }
        }
    }
}
